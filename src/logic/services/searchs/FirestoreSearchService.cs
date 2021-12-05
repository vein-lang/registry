namespace core.services.searchs;

using core.services.searchs.models;
using Google.Cloud.Firestore;
using Newtonsoft.Json;
using NuGet.Versioning;

public class FireOperationBuilder
{
    public CollectionReference PackagesReference { get; private set; }
    public CollectionReference PackagesLinks { get; private set; }

    public FireOperationBuilder(FirestoreDb firestore)
    {
        PackagesReference = firestore.Collection("packages");
        PackagesLinks = firestore.Collection("packages-links");
    }

    public async Task<PackageEntity?> Retrieve(string packageId, NuGetVersion packageVersion)
    {
        if (packageId == null) throw new ArgumentNullException(nameof(packageId));
        if (packageVersion == null) throw new ArgumentNullException(nameof(packageVersion));

        var result = await PackagesReference
                .Document(packageId)
                .Collection("v")
                .Document(packageVersion.ToNormalizedString())
                .GetSnapshotAsync();
        return result?.ConvertTo<PackageEntity>();
    }

    public Task<WriteResult> AddPackage(Package package)
    {
        if (package == null) throw new ArgumentNullException(nameof(package));

        var version = package.Version;
        var normalizedVersion = version.ToNormalizedString();

        var entity = new PackageEntity
        {
            Id = package.Name,
            NormalizedVersion = normalizedVersion,
            OriginalVersion = version.ToFullString(),
            Authors = JsonConvert.SerializeObject(package.Authors),
            Description = package.Description,
            Downloads = package.Downloads,
            HasReadme = package.HasReadme,
            IsPreview = package.IsPreview,
            Listed = package.Listed,
            RequireLicenseAcceptance = package.RequireLicenseAcceptance,
            IconUrl = package.Icon,
            License = package.License,
            ProjectUrl = package.HomepageUrl,
            RepositoryUrl = package.Repository,
            Dependencies = SerializeList(package.Dependencies),
            Published = package.Published
        };
        

        return PackagesReference
            .Document(entity.Id)
            .Collection("v")
            .Document(normalizedVersion)
            .CreateAsync(entity);
    }

    public async Task<bool> ExistAsync(string packageId, NuGetVersion packageVersion = null, CancellationToken cancellationToken = default)
    {
        if (packageVersion is null)
        {
            var result = await PackagesReference
                    .Document(packageId).GetSnapshotAsync(cancellationToken);

            return !(result is null) && result.Exists;
        }
        else
        {
            var result = await PackagesReference
                    .Document(packageId)
                    .Collection("v")
                    .Document(packageVersion.ToNormalizedString())
                    .GetSnapshotAsync(cancellationToken);
            return result is not null && result.Exists;
        }
    }

    public async Task<WriteResult> IncrementDownloads(string packageId, NuGetVersion packageVersion)
    {
        var normalizedVersion = packageVersion.ToNormalizedString();
        var package = await Retrieve(packageId, packageVersion);

        return await PackagesReference
            .Document(packageId)
            .Collection("v")
            .Document(normalizedVersion)
            .UpdateAsync("Downloads", package.Downloads + 1);
    }

    public Task<WriteResult> UpdateDownloads(string packageId, NuGetVersion packageVersion, long downloads)
    {
        var normalizedVersion = packageVersion.ToNormalizedString();

        return PackagesReference
            .Document(packageId)
            .Collection("v")
            .Document(normalizedVersion)
            .UpdateAsync("Downloads", downloads);
    }

    public Task<WriteResult> HardDeletePackage(string packageId, NuGetVersion packageVersion)
    {
        var normalizedVersion = packageVersion.ToNormalizedString();

        return PackagesReference
            .Document(packageId)
            .Collection("v")
            .Document(normalizedVersion)
            .DeleteAsync();
    }

    public Task<WriteResult> UnlistPackage(string packageId, NuGetVersion packageVersion)
    {
        var normalizedVersion = packageVersion.ToNormalizedString();

        return PackagesReference
            .Document(packageId)
            .Collection("v")
            .Document(normalizedVersion)
            .UpdateAsync("Listed", false);
    }

    public Task<WriteResult> RelistPackage(string packageId, NuGetVersion packageVersion)
    {
        var normalizedVersion = packageVersion.ToNormalizedString();

        return PackagesReference
            .Document(packageId)
            .Collection("v")
            .Document(normalizedVersion)
            .UpdateAsync("Listed", true);
    }

    private static string SerializeList<TIn>(IList<TIn> objects)
    {
        var data = objects.ToList();

        return JsonConvert.SerializeObject(data);
    }
}

[FirestoreData]
public class PackageEntity
{
    [FirestoreProperty]
    public string NormalizedVersion { get; set; }
    [FirestoreProperty]
    public string Id { get; set; }
    [FirestoreProperty]
    public string OriginalVersion { get; set; }
    [FirestoreProperty]
    public string Authors { get; set; }
    [FirestoreProperty]
    public string Description { get; set; }
    [FirestoreProperty]
    public ulong Downloads { get; set; }
    [FirestoreProperty]
    public bool HasReadme { get; set; }
    [FirestoreProperty]
    public bool IsPreview { get; set; }
    [FirestoreProperty]
    public bool Listed { get; set; }
    [FirestoreProperty]
    public bool RequireLicenseAcceptance { get; set; }
    [FirestoreProperty]
    public string IconUrl { get; set; }
    [FirestoreProperty]
    public string License { get; set; }
    [FirestoreProperty]
    public Uri ProjectUrl { get; set; }
    [FirestoreProperty]
    public Uri RepositoryUrl { get; set; }
    [FirestoreProperty]
    public string Dependencies { get; set; }
    public bool HasEmbeddedIcon => IconUrl?.StartsWith("@/") ?? false;
    [FirestoreProperty]
    public DateTimeOffset Published { get; set; }
}

public class FirestoreSearchService : ISearchService
{
    private static readonly Task<DependentsResponse> EmptyDependentsResponseTask =
            Task.FromResult(new DependentsResponse
            {
                TotalHits = 0,
                Data = new List<DependentResult>()
            });

    private readonly FireOperationBuilder _table;
    private readonly IUrlGenerator _url;

    public FirestoreSearchService(FireOperationBuilder operationBuilder, IUrlGenerator url)
    {
        _table = operationBuilder ?? throw new ArgumentNullException(nameof(operationBuilder));
        _url = url;
    }

    public async Task<SearchResponse> SearchAsync(
        SearchRequest request,
        CancellationToken cancellationToken)
    {
        var results = await SearchInternalAsync(
                request.Query,
                request.Skip,
                request.Take,
                request.IncludePrerelease,
                request.IncludeSemVer2,
                cancellationToken);

        return new SearchResponse
        {
            TotalHits = results.Count,
            Data = results.Select(ToSearchResult).ToList()
        };
    }

    public async Task<AutocompleteResponse> AutocompleteAsync(
        AutocompleteRequest request,
        CancellationToken cancellationToken)
    {
        var results = await SearchInternalAsync(
                request.Query,
                request.Skip,
                request.Take,
                request.IncludePrerelease,
                request.IncludeSemVer2,
                cancellationToken);

        return new AutocompleteResponse
        {
            TotalHits = results.Count,
            Data = results.Select(ToAutocompleteResult).ToList(),
        };
    }

    public Task<AutocompleteResponse> ListPackageVersionsAsync(
        VersionsRequest request,
        CancellationToken cancellationToken)
    {
        // TODO: Support versions autocomplete.
        // See: https://github.com/loic-sharma/BaGet/issues/291
        throw new NotImplementedException();
    }

    public Task<DependentsResponse> FindDependentsAsync(string packageId, CancellationToken cancellationToken)
    {
        return EmptyDependentsResponseTask;
    }

    private async Task<List<List<PackageEntity>>> SearchInternalAsync(
        string searchText,
        int skip,
        int take,
        bool includePrerelease,
        bool includeSemVer2,
        CancellationToken cancellationToken)
    {
        var packages = await _table.PackagesReference
                .ListDocumentsAsync().ToListAsync(cancellationToken);
        packages = packages.Where(x => x.Id.IndexOf(searchText, StringComparison.InvariantCultureIgnoreCase) >= 0)
            .ToList();

        if (packages.Count == 0)
            return new List<List<PackageEntity>>();

        var segment = await packages.ToAsyncEnumerable()
                .Select(x => x.Collection("v"))
                .SelectAwait(async x => await x.Limit(20).GetSnapshotAsync(cancellationToken))
                .SelectMany(x => x.Documents.ToAsyncEnumerable())
                .ToListAsync(cancellationToken);

        string lastPartitionKey = null;
        var results = new List<List<PackageEntity>>();
        foreach (var result in segment)
        {
            var pkgID = result.GetValue<string>("Id");

            if (lastPartitionKey != pkgID)
            {
                results.Add(new List<PackageEntity>());
                lastPartitionKey = pkgID;
            }

            var d2 = result.ConvertTo<PackageEntity>();
            results.Last().Add(d2);
        }

        return results.Skip(skip).Take(take).ToList();
    }

    private string ToAutocompleteResult(IReadOnlyList<PackageEntity> packages)
    {
        // TODO: This should find the latest version and return its package Id.
        return packages.Last().Id;
    }

    private SearchResult ToSearchResult(IReadOnlyList<PackageEntity> packages)
    {
        NuGetVersion latestVersion = null;
        PackageEntity latest = null;
        var versions = new List<SearchResultVersion>();
        ulong totalDownloads = 0;

        foreach (var package in packages)
        {
            var version = NuGetVersion.Parse(package.OriginalVersion);

            totalDownloads += package.Downloads;
            versions.Add(new SearchResultVersion
            {
                RegistrationLeafUrl = "_url.GetRegistrationLeafUrl(package.Id, version)",
                Version = package.NormalizedVersion,
                Downloads = package.Downloads,
            });

            if (latestVersion == null || version > latestVersion)
            {
                latest = package;
                latestVersion = version;
            }
        }

        var iconUrl = latest.HasEmbeddedIcon
                ? _url.GetPackageIconDownloadUrl(latest.Id, latestVersion)
                : latest.IconUrl;

        return new SearchResult
        {
            PackageId = latest.Id,
            Version = latest.NormalizedVersion,
            Description = latest.Description,
            Authors = JsonConvert.DeserializeObject<string[]>(latest.Authors),
            IconUrl = iconUrl,
            LicenseUrl = latest.License,
            ProjectUrl = latest.ProjectUrl?.ToString(),
            RegistrationIndexUrl = "_url.GetRegistrationIndexUrl(latest.Id)",
            //Summary = latest.Summary,
            //Tags = JsonConvert.DeserializeObject<string[]>(latest.Tags),
            Title = latest.Id,
            TotalDownloads = totalDownloads,
            Versions = versions,
        };
    }
}
