namespace core.services;

using core.services.searchs;
using Microsoft.Extensions.Options;
using vein.project.shards;

public class PackageIndexingService : IPackageIndexingService
{
    private readonly IPackageService _packages;
    private readonly IPackageStorageService _storage;
    private readonly ISearchIndexer _search;
    private readonly SystemTime _time;
    private readonly IOptionsSnapshot<RegistryOptions> _options;
    private readonly ILogger<PackageIndexingService> _logger;

    public PackageIndexingService(
        IPackageService packages,
        IPackageStorageService storage,
        ISearchIndexer search,
        SystemTime time,
        IOptionsSnapshot<RegistryOptions> options,
        ILogger<PackageIndexingService> logger)
    {
        _packages = packages ?? throw new ArgumentNullException(nameof(packages));
        _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        _search = search ?? throw new ArgumentNullException(nameof(search));
        _time = time ?? throw new ArgumentNullException(nameof(time));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<PackageIndexingResult> IndexAsync(Stream packageStream, CancellationToken cancellationToken)
    {
        var package = default(Package);
        var readmeStream = default(Stream);
        var iconStream = default(Stream);

        try
        {
            var packageReader = await Shard.OpenAsync(packageStream, true);
            package = Package.CreateFromManifest(await packageReader.GetManifestAsync());
            package.Published = _time.UtcNow;

            //nuspecStream = await packageReader.GetNuspecAsync(cancellationToken);
            //nuspecStream = await nuspecStream.AsTemporaryFileStreamAsync();

            if (package.HasReadme)
            {
                readmeStream = await packageReader.GetReadmeAsync(cancellationToken);
                readmeStream = await readmeStream.AsTemporaryFileStreamAsync();
            }

            if (package.HasEmbeddedIcon)
            {
                iconStream = await packageReader.GetIconAsync(cancellationToken);
                iconStream = await iconStream.AsTemporaryFileStreamAsync();
            }
        }
        catch (ShardPackageCorruptedException e)
        {
            _logger.LogError(e, "Uploaded package is invalid");

            return PackageIndexingResult.InvalidPackage;
        }

        // The package is well-formed. Ensure this is a new package.
        if (await _packages.ExistsAsync(package.Name, package.Version, cancellationToken))
        {
            if (!_options.Value.AllowPackageOverwrites)
            {
                return PackageIndexingResult.PackageAlreadyExists;
            }

            await _packages.HardDeletePackageAsync(package.Name, package.Version, cancellationToken);
            await _storage.DeleteAsync(package.Name, package.Version, cancellationToken);
        }

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
                cancellationToken);
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

        _logger.LogInformation(
            "Persisted package {Id} {Version} content to storage, saving metadata to database...",
            package.Name,
            package.NormalizedVersionString);

        var result = await _packages.AddAsync(package, cancellationToken);
        if (result == PackageAddResult.PackageAlreadyExists)
        {
            _logger.LogWarning(
                "Package {Id} {Version} metadata already exists in database",
                package.Name,
                package.NormalizedVersionString);

            return PackageIndexingResult.PackageAlreadyExists;
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

        await _search.IndexAsync(package, cancellationToken);

        _logger.LogInformation(
            "Successfully indexed package {Id} {Version} in search",
            package.Name,
            package.NormalizedVersionString);

        return PackageIndexingResult.Success;
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
    /// The package has been indexed successfully.
    /// </summary>
    Success,
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
    /// <param name="cancellationToken"></param>
    /// <returns>The result of the attempted indexing operation.</returns>
    Task<PackageIndexingResult> IndexAsync(Stream stream, CancellationToken cancellationToken);
}
