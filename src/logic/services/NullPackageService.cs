namespace core.services;

using NuGet.Versioning;
using System;

public class NullPackageService : IPackageService
{
    public Task<PackageAddResult> AddAsync(Package package, CancellationToken cancellationToken)
        => Task.FromResult(PackageAddResult.Success);

    public Task<PackageAddResult> AddAsync(Package package, RegistryUser owner, CancellationToken token = default)
        => Task.FromResult(PackageAddResult.Success);

    public Task<Package?> FindOrNullAsync(string id, NuGetVersion version, bool includeUnlisted,
        CancellationToken cancellationToken)
        => Task.FromResult(default(Package));

    public async Task<IReadOnlyList<Package>> FindAsync(string id, bool includeUnlisted, CancellationToken cancellationToken)
        => new List<Package>().AsReadOnly();

    public async Task<List<Package>> GetLatestPackagesByUserAsync(RegistryUser user, CancellationToken token = default)
        => new List<Package>();

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

    public async Task<IReadOnlyCollection<string>> GetPopularPackagesAsync() => new List<string>().AsReadOnly();

    public async Task<IReadOnlyCollection<string>> GetLatestPackagesAsync() => new List<string>().AsReadOnly();

    public Task<ulong> GetTotalDownloadsAsync() => Task.FromResult(0ul);

    public Task<ulong> GetPackagesCountAsync() => Task.FromResult(0ul);

    public async Task<IReadOnlyList<Package>> FindForUserAsync(string userID, CancellationToken cancellationToken)
        => new List<Package>().AsReadOnly();
}
