namespace core.services;

using NuGet.Versioning;

/// <summary>
/// The "source of truth" for packages' state. Packages' content
/// are stored by the <see cref="IPackageStorageService"/>.
/// </summary>
public interface IPackageService
{
    /// <summary>
    /// Attempt to add a new package to the database.
    /// </summary>
    /// <param name="package">The package to add to the database.</param>
    /// <param name="owner">A owner of package.</param>
    /// <param name="token">A token to cancel the task.</param>
    /// <returns>The result of attempting to add the package to the database.</returns>
    Task<PackageAddResult> AddAsync(Package package, UserRecord owner, CancellationToken token = default);

    /// <summary>
    /// Attempt to find a package with the given id and version.
    /// </summary>
    /// <param name="id">The package's id.</param>
    /// <param name="version">The package's version.</param>
    /// <param name="includeUnlisted">Whether unlisted results should be included.</param>
    /// <param name="cancellationToken">A token to cancel the task.</param>
    /// <returns>The package found, or null.</returns>
    Task<Package?> FindOrNullAsync(
        string id,
        NuGetVersion version,
        bool includeUnlisted,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Attempt to find all packages with a given id.
    /// </summary>
    /// <param name="id">The packages' id.</param>
    /// <param name="includeUnlisted">Whether unlisted results should be included.</param>
    /// <param name="cancellationToken">A token to cancel the task.</param>
    /// <returns>The packages found. Always non-null.</returns>
    Task<IReadOnlyList<Package>> FindAsync(string id, bool includeUnlisted, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Package>> FindForUserAsync(string userID, CancellationToken cancellationToken = default);

    Task<List<Package>> GetLatestPackagesByUserAsync(UserRecord user, CancellationToken token = default);

    /// <summary>
    /// Determine whether a package exists in the database (even if the package is unlisted).
    /// </summary>
    /// <param name="id">The package id to search.</param>
    /// <param name="cancellationToken">A token to cancel the task.</param>
    /// <returns>Whether the package exists in the database.</returns>
    Task<bool> ExistsAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Determine whether a package exists in the database (even if the package is unlisted).
    /// </summary>
    /// <param name="id">The package id to search.</param>
    /// <param name="version">The package version to search.</param>
    /// <param name="cancellationToken">A token to cancel the task.</param>
    /// <returns>Whether the package exists in the database.</returns>
    Task<bool> ExistsAsync(string id, NuGetVersion version, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unlist a package, making it undiscoverable.
    /// </summary>
    /// <param name="id">The id of the package to unlist.</param>
    /// <param name="version">The version of the package to unlist.</param>
    /// <param name="cancellationToken">A token to cancel the task.</param>
    /// <returns>False if the package does not exist.</returns>
    Task<bool> UnlistPackageAsync(string id, NuGetVersion version, CancellationToken cancellationToken = default);

    /// <summary>
    /// Relist a package, making it discoverable.
    /// </summary>
    /// <param name="id">The id of the package to relist.</param>
    /// <param name="version">The version of the package to relist.</param>
    /// <param name="cancellationToken">A token to cancel the task.</param>
    /// <returns>False if the package does not exist.</returns>
    Task<bool> RelistPackageAsync(string id, NuGetVersion version, CancellationToken cancellationToken = default);

    /// <summary>
    /// Increment a package's download count.
    /// </summary>
    /// <param name="id">The id of the package to update.</param>
    /// <param name="version">The id of the package to update.</param>
    /// <param name="cancellationToken">A token to cancel the task.</param>
    /// <returns>False if the package does not exist.</returns>
    Task<bool> AddDownloadAsync(string id, NuGetVersion version, CancellationToken cancellationToken = default);

    /// <summary>
    /// Completely remove the package from the database.
    /// </summary>
    /// <param name="id">The id of the package to remove.</param>
    /// <param name="version">The version of the pacakge to remove.</param>
    /// <param name="cancellationToken">A token to cancel the task.</param>
    /// <returns>False if the package doesn't exist.</returns>
    Task<bool> HardDeletePackageAsync(string id, NuGetVersion version, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get popular packages from db.
    /// </summary>
    Task<IReadOnlyCollection<string>> GetPopularPackagesAsync();

    /// <summary>
    /// Get latest packages from db.
    /// </summary>
    Task<IReadOnlyCollection<string>> GetLatestPackagesAsync();

    /// <summary>
    /// Get total downloads from db.
    /// </summary>
    Task<ulong> GetTotalDownloadsAsync();

    /// <summary>
    /// Get count of packages from db.
    /// </summary>
    Task<ulong> GetPackagesCountAsync();
}
