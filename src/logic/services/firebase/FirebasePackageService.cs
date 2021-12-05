namespace core.services;

using core.services.searchs;
using Google.Cloud.Firestore;
using Newtonsoft.Json;
using NuGet.Versioning;
using vein.project;

/// <summary>
/// Stores the metadata of packages using Azure Table Storage.
/// </summary>
public partial class FirebasePackageService : IPackageService
{
    private const int MaxPreconditionFailures = 5;

    private readonly FireOperationBuilder _operationBuilder;
    private readonly ILogger<FirebasePackageService> _logger;

    public FirebasePackageService(
        FireOperationBuilder operationBuilder,
        ILogger<FirebasePackageService> logger)
    {
        _operationBuilder = operationBuilder ?? throw new ArgumentNullException(nameof(operationBuilder));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<PackageAddResult> AddAsync(Package package, CancellationToken cancellationToken)
    {
        try
        {
            var operation = await _operationBuilder.AddPackage(package);
        }
        catch (Exception e)
        {
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
            .Select(AsPackage)
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
        var a = filter.Select(x => x.ConvertTo<PackageEntity>()).Select(AsPackage).ToList();


        return a
            .OrderBy(p => p.Version)
            .ToList()
            .AsReadOnly();
    }

    public async Task<Package?> FindOrNullAsync(
        string id,
        NuGetVersion version,
        bool includeUnlisted,
        CancellationToken cancellationToken)
    {
        var entity = await _operationBuilder.Retrieve(id, version);

        if (entity == null)
            return null;
        if (!includeUnlisted && !entity.Listed)
            return null;

        return AsPackage(entity);
    }

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

    private Package AsPackage(PackageEntity entity) => new Package
    {
        Name = entity.Id,
        NormalizedVersionString = entity.NormalizedVersion,
        OriginalVersionString = entity.OriginalVersion,

        Authors = JsonConvert.DeserializeObject<List<string>>(entity.Authors),
        Description = entity.Description,
        Downloads = entity.Downloads,
        HasReadme = entity.HasReadme,
        IsPreview = entity.IsPreview,
        Listed = entity.Listed,
        RequireLicenseAcceptance = entity.RequireLicenseAcceptance,
        Icon = entity.IconUrl,
        License = entity.License,
        HomepageUrl = entity.ProjectUrl,
        Repository = entity.RepositoryUrl,
        //Tags = JsonConvert.DeserializeObject<string[]>(entity.Tags),
        Dependencies = ParseDependencies(entity.Dependencies),
        //PackageTypes = ParsePackageTypes(entity.PackageTypes),
        //TargetFrameworks = targetFrameworks,
    };

    private Uri? ParseUri(string input) =>
        string.IsNullOrEmpty(input) ? null : new Uri(input);

    private List<PackageReference> ParseDependencies(string input) =>
        JsonConvert.DeserializeObject<List<PackageReference>>(input)
            .ToList();

    //private List<PackageType> ParsePackageTypes(string input)
    //{
    //    return JsonConvert.DeserializeObject<List<PackageTypeModel>>(input)
    //        .Select(e => new PackageType
    //        {
    //            Name = e.Name,
    //            Version = e.Version
    //        })
    //        .ToList();
    //}
}
