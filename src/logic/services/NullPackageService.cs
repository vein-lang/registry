namespace core.services;

using NuGet.Versioning;

public class NullPackageService : IPackageService
{
    public Task<PackageAddResult> AddAsync(Package package, CancellationToken cancellationToken)
        => Task.FromResult(PackageAddResult.Success);

    public Task<Package?> FindOrNullAsync(string id, NuGetVersion version, bool includeUnlisted,
        CancellationToken cancellationToken)
        => Task.FromResult(default(Package));

    public async Task<IReadOnlyList<Package>> FindAsync(string id, bool includeUnlisted, CancellationToken cancellationToken)
        => new List<Package>().AsReadOnly();

    public Task<bool> ExistsAsync(string id, CancellationToken cancellationToken)
        => Task.FromResult(default(bool));

    public Task<bool> ExistsAsync(string id, NuGetVersion version, CancellationToken cancellationToken)
        => Task.FromResult(default(bool));

    public Task<bool> UnlistPackageAsync(string id, NuGetVersion version, CancellationToken cancellationToken)
        => Task.FromResult(default(bool));

    public Task<bool> RelistPackageAsync(string id, NuGetVersion version, CancellationToken cancellationToken)
        => Task.FromResult(default(bool));

    public Task<bool> AddDownloadAsync(string id, NuGetVersion version, CancellationToken cancellationToken)
        => Task.FromResult(default(bool));

    public Task<bool> HardDeletePackageAsync(string id, NuGetVersion version, CancellationToken cancellationToken)
        => Task.FromResult(default(bool));
}
