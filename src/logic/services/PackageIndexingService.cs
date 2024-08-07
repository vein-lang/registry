namespace core.services;

using AutoMapper;
using searchs;
using Microsoft.Extensions.Options;
using vein.project.shards;

public class PackageIndexingService(
    IPackageService packages,
    IPackageStorageService storage,
    ISearchIndexer search,
    SystemTime time,
    IOptionsSnapshot<RegistryOptions> options,
    ILogger<PackageIndexingService> logger,
    IUserService userService,
    IMapper mapper)
    : IPackageIndexingService
{
    private readonly IPackageService _packages = packages ?? throw new ArgumentNullException(nameof(packages));
    private readonly IPackageStorageService _storage = storage ?? throw new ArgumentNullException(nameof(storage));
    private readonly ISearchIndexer _search = search ?? throw new ArgumentNullException(nameof(search));
    private readonly SystemTime _time = time ?? throw new ArgumentNullException(nameof(time));
    private readonly IOptionsSnapshot<RegistryOptions> _options = options ?? throw new ArgumentNullException(nameof(options));
    private readonly ILogger<PackageIndexingService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task<(PackageIndexingResult, string)> IndexAsync(Stream packageStream, UserRecord publisher, CancellationToken token = default)
    {
        var package = default(Package);
        var readmeStream = default(Stream);
        var iconStream = default(Stream);

        try
        {
            var packageReader = await Shard.OpenAsync(packageStream, true, token);
            var manifest = await packageReader.GetManifestAsync(token);

            if (manifest.IsWorkload && !await userService.UserAllowedPublishWorkloads())
                return (PackageIndexingResult.InvalidPackage, "You cannot publish workload package!");

            package = mapper.Map<Package>(manifest);
            package.Published = _time.UtcNow;
            package.Listed = true;

            if (package.HasEmbbededReadme)
                readmeStream = await packageReader.GetReadmeAsync(token);
            if (package.HasEmbeddedIcon)
                iconStream = await packageReader.GetIconAsync(token);
            await PackageValidator.ValidateExistAsync(packageReader);
        }
        catch (ShardPackageCorruptedException e) when (e.InnerException is PackageValidatorException validateError)
        {
            _logger.LogError(e, "Uploaded package is invalid");
            _logger.LogError(e.Message);

            return (PackageIndexingResult.InvalidPackage, validateError.Message);
        }
        catch (PackageValidatorException e)
        {
            _logger.LogError(e, "Validation failed");
            _logger.LogError(e.Message);

            return (PackageIndexingResult.InvalidPackage, e.Message);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Uploaded package is invalid");

            return (PackageIndexingResult.InternalError, "unknown error");
        }

        // The package is well-formed. Ensure this is a new package.
        if (await _packages.ExistsAsync(package.Name, package.Version, token))
        {
            if (!_options.Value.AllowPackageOverwrites)
            {
                return (PackageIndexingResult.PackageAlreadyExists, "");
            }

            await _packages.HardDeletePackageAsync(package.Name, package.Version, token);
            await _storage.DeleteAsync(package.Name, package.Version, token);
        }

        _logger.LogInformation(
            "Persisted package {Id} {Version} content to storage, saving metadata to database...",
            package.Name,
            package.NormalizedVersionString);

        var result = await _packages.AddAsync(package, publisher, token);

        if (result == PackageAddResult.InternalError)
            return (PackageIndexingResult.InternalError, "");

        if (result == PackageAddResult.PackageAlreadyExists)
        {
            _logger.LogWarning(
                "Package {Id} {Version} metadata already exists in database",
                package.Name,
                package.NormalizedVersionString);

            return (PackageIndexingResult.PackageAlreadyExists, "");
        }

        if (result == PackageAddResult.AccessDenied)
        {
            _logger.LogWarning(
                "Package {Id} {Version} owner and publisher is not matched ID in database",
                package.Name,
                package.NormalizedVersionString);

            return (PackageIndexingResult.AccessDenied, "");
        }

        if (result != PackageAddResult.Success)
        {
            _logger.LogError($"Unknown {nameof(PackageAddResult)} value: {{PackageAddResult}}", result);

            throw new InvalidOperationException($"Unknown {nameof(PackageAddResult)} value: {result}");
        }

        _logger.LogInformation(
            "Successfully persisted package {Id} {Version} metadata to database. Indexing in search...",
            package.Name,
            package.NormalizedVersionString);
        
        // TODO: Add more package validations
        // TODO: Call PackageArchiveReader.ValidatePackageEntriesAsync
        _logger.LogInformation(
            "Validated package {PackageId} {PackageVersion}, persisting content to storage...",
            package.Name,
            package.NormalizedVersionString);

        try
        {
            packageStream.Position = 0;

            await _storage.SavePackageContentAsync(
                package,
                packageStream,
                readmeStream,
                iconStream,
                token);
        }
        catch (Exception e)
        {
            // This may happen due to concurrent pushes.
            // TODO: Make IPackageStorageService.SavePackageContentAsync return a result enum so this
            // can be properly handled.
            _logger.LogError(
                e,
                "Failed to persist package {PackageId} {PackageVersion} content to storage",
                package.Name,
                package.NormalizedVersionString);

            throw;
        }
        
        await _search.IndexAsync(package, token);

        _logger.LogInformation(
            "Successfully indexed package {Id} {Version} in search",
            package.Name,
            package.NormalizedVersionString);

        return (PackageIndexingResult.Success, "");
    }
}


/// <summary>
/// The result of attempting to index a package.
/// See <see cref="IPackageIndexingService.IndexAsync(Stream, CancellationToken)"/>.
/// </summary>
public enum PackageIndexingResult
{
    /// <summary>
    /// The package is malformed. This may also happen if BaGet is in a corrupted state.
    /// </summary>
    InvalidPackage,

    /// <summary>
    /// The package has already been indexed.
    /// </summary>
    PackageAlreadyExists,

    /// <summary>
    /// Access denied.
    /// </summary>
    AccessDenied,

    /// <summary>
    /// The package has been indexed successfully.
    /// </summary>
    Success,

    /// <summary>
    /// Package publish has failed.
    /// </summary>
    InternalError,
}

/// <summary>
/// The service used to accept new packages.
/// </summary>
public interface IPackageIndexingService
{
    /// <summary>
    /// Attempt to index a new package.
    /// </summary>
    /// <param name="stream">The stream containing the package's content.</param>
    /// <param name="publisher">A publisher user.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>The result of the attempted indexing operation.</returns>
    Task<(PackageIndexingResult, string)> IndexAsync(Stream packageStream, UserRecord publisher,
        CancellationToken token = default);
}
