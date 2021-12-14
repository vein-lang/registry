namespace core.services;

using System.Text;
using Newtonsoft.Json;
using NuGet.Versioning;

public class PackageStorageService : IPackageStorageService
{
    private const string PackagesPathPrefix = "packages";

    private const string PackageContentType = "binary/octet-stream";
    private const string NuspecContentType = "text/plain";
    private const string ReadmeContentType = "text/markdown";
    private const string IconContentType = "image/xyz";

    private readonly IStorageService _storage;
    private readonly ILogger<PackageStorageService> _logger;

    public PackageStorageService(
        IStorageService storage,
        ILogger<PackageStorageService> logger)
    {
        _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task SavePackageContentAsync(
        Package package,
        Stream packageStream,
        Stream? readmeStream,
        Stream? iconStream,
        CancellationToken cancellationToken = default)
    {
        package = package ?? throw new ArgumentNullException(nameof(package));
        packageStream = packageStream ?? throw new ArgumentNullException(nameof(packageStream));

        var lowercasedId = package.Name.ToLowerInvariant();
        var lowercasedNormalizedVersion = package.Version.ToNormalizedString()?.ToLowerInvariant();

        var packagePath = PackagePath(lowercasedId, lowercasedNormalizedVersion ?? "");
        var specPath = VeinSpecPath(lowercasedId, lowercasedNormalizedVersion ?? "");
        var readmePath = ReadmePath(lowercasedId, lowercasedNormalizedVersion ?? "");
        var iconPath = IconPath(lowercasedId, lowercasedNormalizedVersion ?? "");
        
        _logger.LogInformation(
            "Storing package {PackageId} {PackageVersion} at {Path}...",
            lowercasedId,
            lowercasedNormalizedVersion,
            packagePath);

        // Store the package.
        var result = await _storage.PutAsync(packagePath, packageStream, PackageContentType, cancellationToken);
        if (result == StoragePutResult.Conflict)
        {
            // TODO: This should be returned gracefully with an enum.
            _logger.LogInformation(
                "Could not store package {PackageId} {PackageVersion} at {Path} due to conflict",
                lowercasedId,
                lowercasedNormalizedVersion,
                packagePath);

            throw new InvalidOperationException($"Failed to store package {lowercasedId} {lowercasedNormalizedVersion} due to conflict");
        }

        // Store the package's nuspec.
        _logger.LogInformation(
            "Storing package {PackageId} {PackageVersion} nuspec at {Path}...",
            lowercasedId,
            lowercasedNormalizedVersion,
            specPath);
        var specContent = JsonConvert.SerializeObject(package);
        using var specStream = new MemoryStream(Encoding.UTF8.GetBytes(specContent));
        result = await _storage.PutAsync(specPath, specStream, NuspecContentType, cancellationToken);
        if (result == StoragePutResult.Conflict)
        {
            // TODO: This should be returned gracefully with an enum.
            _logger.LogInformation(
                "Could not store package {PackageId} {PackageVersion} nuspec at {Path} due to conflict",
                lowercasedId,
                lowercasedNormalizedVersion,
                specPath);

            throw new InvalidOperationException($"Failed to store package {lowercasedId} {lowercasedNormalizedVersion} nuspec due to conflict");
        }

        // Store the package's readme, if one exists.
        if (readmeStream != null)
        {
            _logger.LogInformation(
                "Storing package {PackageId} {PackageVersion} readme at {Path}...",
                lowercasedId,
                lowercasedNormalizedVersion,
                readmePath);
            readmeStream.Seek(0, SeekOrigin.Begin);
            result = await _storage.PutAsync(readmePath, readmeStream, ReadmeContentType, cancellationToken);
            if (result == StoragePutResult.Conflict)
            {
                // TODO: This should be returned gracefully with an enum.
                _logger.LogInformation(
                    "Could not store package {PackageId} {PackageVersion} readme at {Path} due to conflict",
                    lowercasedId,
                    lowercasedNormalizedVersion,
                    readmePath);

                throw new InvalidOperationException($"Failed to store package {lowercasedId} {lowercasedNormalizedVersion} readme due to conflict");
            }
        }

        // Store the package's icon, if one exists.
        if (iconStream != null)
        {
            _logger.LogInformation(
                "Storing package {PackageId} {PackageVersion} icon at {Path}...",
                lowercasedId,
                lowercasedNormalizedVersion,
                iconPath);
            iconStream.Seek(0, SeekOrigin.Begin);
            result = await _storage.PutAsync(iconPath, iconStream, IconContentType, cancellationToken);
            if (result == StoragePutResult.Conflict)
            {
                // TODO: This should be returned gracefully with an enum.
                _logger.LogInformation(
                    "Could not store package {PackageId} {PackageVersion} icon at {Path} due to conflict",
                    lowercasedId,
                    lowercasedNormalizedVersion,
                    iconPath);

                throw new InvalidOperationException($"Failed to store package {lowercasedId} {lowercasedNormalizedVersion} icon");
            }
        }

        _logger.LogInformation(
            "Finished storing package {PackageId} {PackageVersion}",
            lowercasedId,
            lowercasedNormalizedVersion);
    }

    public async Task<Stream> GetPackageStreamAsync(string id, NuGetVersion version, CancellationToken cancellationToken)
        => await GetStreamAsync(id, version, PackagePath, cancellationToken);

    public async Task<Stream> GetNuspecStreamAsync(string id, NuGetVersion version, CancellationToken cancellationToken)
        => await GetStreamAsync(id, version, VeinSpecPath, cancellationToken);

    public async Task<Stream> GetReadmeStreamAsync(string id, NuGetVersion version, CancellationToken cancellationToken)
        => await GetStreamAsync(id, version, ReadmePath, cancellationToken);

    public async Task<Stream> GetIconStreamAsync(string id, NuGetVersion version, CancellationToken cancellationToken)
        => await GetStreamAsync(id, version, IconPath, cancellationToken);

    public async Task DeleteAsync(string id, NuGetVersion version, CancellationToken cancellationToken)
    {
        var lowercasedId = id.ToLowerInvariant();
        var lowercasedNormalizedVersion = version.ToNormalizedString().ToLowerInvariant();

        var packagePath = PackagePath(lowercasedId, lowercasedNormalizedVersion);
        var nuspecPath = VeinSpecPath(lowercasedId, lowercasedNormalizedVersion);
        var readmePath = ReadmePath(lowercasedId, lowercasedNormalizedVersion);
        var iconPath = IconPath(lowercasedId, lowercasedNormalizedVersion);

        await _storage.DeleteAsync(packagePath, cancellationToken);
        await _storage.DeleteAsync(nuspecPath, cancellationToken);
        await _storage.DeleteAsync(readmePath, cancellationToken);
        await _storage.DeleteAsync(iconPath, cancellationToken);
    }

    private async Task<Stream> GetStreamAsync(
        string id,
        NuGetVersion version,
        Func<string, string, string> pathFunc,
        CancellationToken cancellationToken)
    {
        var lowercasedId = id.ToLowerInvariant();
        var lowercasedNormalizedVersion = version.ToNormalizedString().ToLowerInvariant();
        var path = pathFunc(lowercasedId, lowercasedNormalizedVersion);

        try
        {
            return await _storage.GetAsync(path, cancellationToken);
        }
        catch (DirectoryNotFoundException)
        {
            _logger.LogError(
                $"Unable to find the '{PackagesPathPrefix}' folder. " +
                "If you've recently upgraded BaGet, please make sure this folder starts with a lowercased letter. ");
            throw;
        }
    }

    private string PackagePath(string lowercasedId, string lowercasedNormalizedVersion) =>
        Path.Combine(
            PackagesPathPrefix,
            lowercasedId,
            lowercasedNormalizedVersion,
            $"{lowercasedId}.{lowercasedNormalizedVersion}.shard");

    private string VeinSpecPath(string lowercasedId, string lowercasedNormalizedVersion) =>
        Path.Combine(
            PackagesPathPrefix,
            lowercasedId,
            lowercasedNormalizedVersion,
            $"{lowercasedId}.vspec");

    private string ReadmePath(string lowercasedId, string lowercasedNormalizedVersion) =>
        Path.Combine(
            PackagesPathPrefix,
            lowercasedId,
            lowercasedNormalizedVersion,
            "readme.md");

    private string IconPath(string lowercasedId, string lowercasedNormalizedVersion) =>
        Path.Combine(
            PackagesPathPrefix,
            lowercasedId,
            lowercasedNormalizedVersion,
            "icon.png");
}


/// <summary>
/// Stores packages' content. Packages' state are stored by the
/// <see cref="IPackageService"/>.
/// </summary>
public interface IPackageStorageService
{
    /// <summary>
    /// Persist a package's content to storage. This operation MUST fail if a package
    /// with the same id/version but different content has already been stored.
    /// </summary>
    /// <param name="package">The package's metadata.</param>
    /// <param name="packageStream">The package's nupkg stream.</param>
    /// <param name="nuspecStream">The package's nuspec stream.</param>
    /// <param name="readmeStream">The package's readme stream, or null if none.</param>
    /// <param name="iconStream">The package's icon stream, or null if none.</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task SavePackageContentAsync(
        Package package,
        Stream packageStream,
        Stream? readmeStream,
        Stream? iconStream,
        CancellationToken cancellationToken);

    /// <summary>
    /// Retrieve a package's nupkg stream.
    /// </summary>
    /// <param name="id">The package's id.</param>
    /// <param name="version">The package's version.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>The package's nupkg stream.</returns>
    Task<Stream> GetPackageStreamAsync(string id, NuGetVersion version, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieve a package's nuspec stream.
    /// </summary>
    /// <param name="id">The package's id.</param>
    /// <param name="version">The package's version.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>The package's nuspec stream.</returns>
    Task<Stream> GetNuspecStreamAsync(string id, NuGetVersion version, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieve a package's readme stream.
    /// </summary>
    /// <param name="id">The package's id.</param>
    /// <param name="version">The package's version.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>The package's readme stream.</returns>
    Task<Stream> GetReadmeStreamAsync(string id, NuGetVersion version, CancellationToken cancellationToken);

    Task<Stream> GetIconStreamAsync(string id, NuGetVersion version, CancellationToken cancellationToken);

    /// <summary>
    /// Remove a package's content from storage. This operation SHOULD succeed
    /// even if the package does not exist.
    /// </summary>
    /// <param name="id">The package's id.</param>
    /// <param name="version">The package's version.</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task DeleteAsync(string id, NuGetVersion version, CancellationToken cancellationToken);
}
