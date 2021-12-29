namespace core.services;

using Newtonsoft.Json;
using NuGet.Versioning;

public class PackageContentService : IPackageContentService
{
    private readonly IPackageService _packages;
    private readonly IPackageStorageService _storage;

    public PackageContentService(
        IPackageService packages,
        IPackageStorageService storage)
    {
        _packages = packages;
        _storage = storage;
    }
    public async Task<PackageVersionsResponse> GetPackageVersionsOrNullAsync(
            string id,
            CancellationToken cancellationToken = default)
    {
        // Fallback to the local packages if mirroring is disabled.
        var packages = await _packages.FindAsync(id, includeUnlisted: true, cancellationToken);

        if (!packages.Any())
        {
            return null;
        }

        var versions = packages.Select(p => p.Version).ToList();

        return new PackageVersionsResponse
        {
            Versions = versions
                .Select(v => v.ToNormalizedString())
                .Select(v => v.ToLowerInvariant())
                .ToList()
        };
    }

    public async Task<Stream> GetPackageContentStreamOrNullAsync(
        string id,
        NuGetVersion version,
        CancellationToken cancellationToken = default)
    {
        var package = await _packages.FindOrNullAsync(id, version, includeUnlisted: true, cancellationToken);

        if (package is null)
            return null;

        if (!await _packages.AddDownloadAsync(id, version, cancellationToken))
            return null;
       

        return await _storage.GetPackageStreamAsync(package.Name, package.Version, cancellationToken);
    }

    public async Task<Stream> GetPackageManifestStreamOrNullAsync(string id, NuGetVersion version, CancellationToken cancellationToken = default)
    {
        var package = await _packages.FindOrNullAsync(id, version, includeUnlisted: true, cancellationToken);
        if (package == null)
            return null;

        return await _storage.GetNuspecStreamAsync(package.Name, package.Version, cancellationToken);
    }

    public async Task<Stream> GetPackageReadmeStreamOrNullAsync(string id, NuGetVersion version, CancellationToken cancellationToken = default)
    {
        var package = await _packages.FindOrNullAsync(id, version, includeUnlisted: true, cancellationToken);
        if (!package.HasEmbbededReadme)
            return null;

        return await _storage.GetReadmeStreamAsync(package.Name, package.Version, cancellationToken);
    }

    public async Task<Stream> GetPackageIconStreamOrNullAsync(string id, NuGetVersion version, CancellationToken cancellationToken = default)
    {
        var package = await _packages.FindOrNullAsync(id, version, includeUnlisted: true, cancellationToken);
        if (package is null)
            return null;
        if (!package?.HasEmbeddedIcon ?? true)
            return null;

        return await _storage.GetIconStreamAsync(package.Name, package.Version, cancellationToken);
    }
}


/// <summary>
/// The Package Content resource, used to download NuGet packages and to fetch other metadata.
/// </summary>
public interface IPackageContentService
{
    /// <summary>
    /// Get a package's versions, or null if the package does not exist.
    /// </summary>
    /// <param name="packageId">The package ID.</param>
    /// <param name="cancellationToken">A token to cancel the task.</param>
    /// <returns>The package's versions, or null if the package does not exist.</returns>
    Task<PackageVersionsResponse> GetPackageVersionsOrNullAsync(
        string packageId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Download a package, or null if the package does not exist.
    /// </summary>
    /// <param name="packageId">The package ID.</param>
    /// <param name="packageVersion">The package's version.</param>
    /// <param name="cancellationToken">A token to cancel the task.</param>
    /// <returns>
    /// The package's content stream, or null if the package does not exist. The stream may not be seekable.
    /// </returns>
    Task<Stream> GetPackageContentStreamOrNullAsync(
        string packageId,
        NuGetVersion packageVersion,
        CancellationToken cancellationToken);

    /// <summary>
    /// Download a package's manifest (nuspec), or null if the package does not exist.
    /// </summary>
    /// <param name="packageId">The package id.</param>
    /// <param name="packageVersion">The package's version.</param>
    /// <param name="cancellationToken">A token to cancel the task.</param>
    /// <returns>
    /// The package's manifest stream, or null if the package does not exist. The stream may not be seekable.
    /// </returns>
    Task<Stream> GetPackageManifestStreamOrNullAsync(
        string packageId,
        NuGetVersion packageVersion,
        CancellationToken cancellationToken);

    /// <summary>
    /// Download a package's readme, or null if the package or readme does not exist.
    /// </summary>
    /// <param name="id">The package id.</param>
    /// <param name="version">The package's version.</param>
    /// <param name="cancellationToken">A token to cancel the task.</param>
    /// <returns>
    /// The package's readme stream, or null if the package or readme does not exist. The stream may not be seekable.
    /// </returns>
    Task<Stream> GetPackageReadmeStreamOrNullAsync(
        string id,
        NuGetVersion version,
        CancellationToken cancellationToken);

    /// <summary>
    /// Download a package's icon, or null if the package or icon does not exist.
    /// </summary>
    /// <param name="id">The package id.</param>
    /// <param name="version">The package's version.</param>
    /// <param name="cancellationToken">A token to cancel the task.</param>
    /// <returns>
    /// The package's icon stream, or null if the package or icon does not exist. The stream may not be seekable.
    /// </returns>
    Task<Stream> GetPackageIconStreamOrNullAsync(
        string id,
        NuGetVersion version,
        CancellationToken cancellationToken);
}


/// <summary>
/// The full list of versions for a single package.
/// </summary>
public class PackageVersionsResponse
{
    /// <summary>
    /// The versions, lowercased and normalized.
    /// </summary>
    [JsonProperty("versions")]
    public IReadOnlyList<string> Versions { get; set; }
}
