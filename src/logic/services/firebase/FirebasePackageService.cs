namespace core.services;

using AutoMapper;
using core.services.searchs;
using Google.Cloud.Firestore;
using MoreLinq;
using NuGet.Versioning;

/// <summary>
/// Stores the metadata of packages using Azure Table Storage.
/// </summary>
public partial class FirebasePackageService : IPackageService
{
    private const int MaxPreconditionFailures = 5;

    private readonly FireOperationBuilder _operationBuilder;
    private readonly ILogger<FirebasePackageService> _logger;
    private readonly IMapper _mapper;
    private readonly IUrlGenerator _urlGenerator;

    public FirebasePackageService(
        FireOperationBuilder operationBuilder,
        ILogger<FirebasePackageService> logger,
        IMapper mapper,
        IUrlGenerator urlGenerator)
    {
        _operationBuilder = operationBuilder ?? throw new ArgumentNullException(nameof(operationBuilder));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _mapper = mapper;
        _urlGenerator = urlGenerator;
    }

    public async Task<PackageAddResult> AddAsync(Package package, RegistryUser owner, CancellationToken token = default)
    {
        try
        {
            var operation = await this
                ._operationBuilder
                .AddPackage(package, owner);
        }
        catch (OwnerIsNotMatchException)
        {
            return PackageAddResult.AccessDenied;
        }
        catch (Exception e)
        {
            this._logger.LogError(e, nameof(AddAsync));
            return PackageAddResult.PackageAlreadyExists;
        }

        return PackageAddResult.Success;
    }

    public async Task<bool> AddDownloadAsync(
        string id,
        NuGetVersion version,
        CancellationToken cancellationToken)
    {
        var attempt = 0;

        while (true)
        {
            try
            {
                attempt++;
                await _operationBuilder.IncrementDownloads(id, version);
                return true;
            }
            catch (Exception e)
                when (attempt < MaxPreconditionFailures)
            {
                _logger.LogWarning(
                    e,
                    $"Retrying due to precondition failure, attempt {attempt} of {MaxPreconditionFailures}..");
            }
        }
    }

    public Task<bool> ExistsAsync(string id, CancellationToken cancellationToken) =>
        _operationBuilder.ExistAsync(id, null, cancellationToken);

    public Task<bool> ExistsAsync(
        string id,
        NuGetVersion version,
        CancellationToken cancellationToken)
        => _operationBuilder.ExistAsync(id, version, cancellationToken);

    public async Task<IReadOnlyList<Package>> FindForUserAsync(string userID, CancellationToken cancellationToken)
    {
        var packagesLinkQuery = await _operationBuilder.PackagesLinks
            .WhereEqualTo(nameof(PackageLink.UserID), $"{userID}")
            .GetSnapshotAsync(cancellationToken);

        var links = packagesLinkQuery.Select(x => x.ConvertTo<PackageLink>().PackageID).ToList();

        var packages = await
            _operationBuilder.PackagesReference
                .ListDocumentsAsync()
                .Where(x => links.Contains(x.Id))
                .SelectAwait(async x => await x.GetSnapshotAsync())
                .ToListAsync(cancellationToken);

        return packages
            .Select(x => x.ConvertTo<PackageEntity>())
            .Select(x => _mapper.Map<Package>(x))
            .OrderBy(x => x.Name)
            .ToList()
            .AsReadOnly();
    }

    public async Task<IReadOnlyList<Package>> FindAsync(string id, bool includeUnlisted, CancellationToken cancellationToken)
    {
        var packages = await
                _operationBuilder.PackagesReference.ListDocumentsAsync()
                    .FirstOrDefaultAsync(x =>
                        string.Equals(x.Id, id, StringComparison.InvariantCultureIgnoreCase), cancellationToken);

        var filter = await packages
                .Collection("v")
                .WhereEqualTo("Listed", !includeUnlisted)
                .Limit(500)
                .GetSnapshotAsync(cancellationToken);
        var a = filter.Select(x => x.ConvertTo<PackageEntity>()).Select(x => _mapper.Map<Package>(x)).ToList();


        return a
            .OrderBy(p => p.Version)
            .ToList()
            .AsReadOnly();
    }

    private static Dictionary<(string, NuGetVersion), Package> _cachePackages = new ();


    public async Task<Package?> FindOrNullAsync(
        string id,
        NuGetVersion version,
        bool includeUnlisted,
        CancellationToken cancellationToken)
    {
        if (!version.Metadata.Equals("next") || !version.Metadata.Equals("latest"))
            if (_cachePackages.ContainsKey((id, version)))
                return _cachePackages[(id, version)];

        var entity = await _operationBuilder.Retrieve(id, version);

        if (entity == null)
            return null;
        if (!includeUnlisted && !entity.Listed)
            return null;

        var result = _mapper.Map<Package>(entity);

        if (!version.Metadata.Equals("next") || !version.Metadata.Equals("latest"))
            _cachePackages[(id, version)] = result;
        return result;
    }


    public async Task<IReadOnlyCollection<string>> GetPopularPackagesAsync() =>
        (await _operationBuilder.PackagesReference.ListDocumentsAsync()
            .SelectAwait(async x => ((await x
                    .Collection("v")
                    .GetSnapshotAsync())
                .Sum(x => x.GetValue<long>(nameof(PackageEntity.Downloads))), x.Id))
            .OrderByDescending(x => x.Item1)
            .Select(x => x.Id)
            .Take(10)
            .ToListAsync())
        .AsReadOnly();

    public async Task<IReadOnlyCollection<string>> GetLatestPackagesAsync() =>
        (await _operationBuilder.PackagesReference
            .ListDocumentsAsync()
            .SelectAwait(async x => await x.GetSnapshotAsync())
            .Where(x => x.UpdateTime is not null)
            .Select(x => (x.UpdateTime, x.Id))
            .OrderByDescending(x => x.UpdateTime!.Value.ToDateTimeOffset())
            .Take(10)
            .Select(x => x.Id)
            .ToListAsync())
        .AsReadOnly();

    public async Task<ulong> GetPackagesCountAsync() => (ulong)
        await _operationBuilder
            .PackagesReference
            .ListDocumentsAsync()
            .CountAsync();

    public async Task<List<Package>> GetLatestPackagesByUserAsync(RegistryUser user, CancellationToken token = default)
    {
        var snap = await _operationBuilder
            .PackagesReference
            .WhereEqualTo("owner", user.UID)
            .OrderBy(FieldPath.DocumentId)
            .Select("latest")
            .GetSnapshotAsync();
        
        var r1 = snap
            .ToList()
            .Select(x => x.GetValue<DocumentReference>("latest"))
            .Select(x => x.GetSnapshotAsync());
        var r2 = await Task.WhenAll(r1);
        var r3 = r2
            .Select(x => _mapper.Map<Package>(x.ConvertTo<PackageEntity>()))
            .Pipe(x => x.Icon = x.HasEmbeddedIcon ? _urlGenerator.GetPackageIconDownloadUrl(x.Name, x.Version) : x.Icon)
            .ToList();
        return r3;
    }

    public async Task<ulong> GetTotalDownloadsAsync() => (ulong)await _operationBuilder
        .PackagesReference
        .ListDocumentsAsync()
        .SelectAwait(async x => await x.GetSnapshotAsync())
        .SumAsync(x => x.ContainsField("TotalDownloads") ? x.GetValue<long>("TotalDownloads") : 0l);


    public async Task<bool> HardDeletePackageAsync(string id, NuGetVersion version, CancellationToken cancellationToken)
        => await TryUpdatePackageAsync(x =>
                _operationBuilder.HardDeletePackage(id, version),
            cancellationToken);

    public async Task<bool> RelistPackageAsync(string id, NuGetVersion version, CancellationToken cancellationToken) =>
        await TryUpdatePackageAsync(x =>
                _operationBuilder.RelistPackage(id, version),
            cancellationToken);

    public async Task<bool> UnlistPackageAsync(string id, NuGetVersion version, CancellationToken cancellationToken) =>
        await TryUpdatePackageAsync(x =>
                _operationBuilder.UnlistPackage(id, version),
            cancellationToken);

    private async Task<bool> TryUpdatePackageAsync(Func<CancellationToken, Task<WriteResult>> operation, CancellationToken cancellationToken)
    {
        try
        {
            await operation(cancellationToken);
        }
        catch (Exception e)
        {
            return false;
        }

        return true;
    }
    
    private Uri? ParseUri(string input) =>
        string.IsNullOrEmpty(input) ? null : new Uri(input);
}
