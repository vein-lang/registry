namespace core.services.searchs;

using System.Diagnostics.CodeAnalysis;
using AutoMapper;
using core.services.searchs.models;
using Google.Cloud.Firestore;
using Newtonsoft.Json;
using NuGet.Versioning;

public class OwnerIsNotMatchException : Exception
{

}

public class FireOperationBuilder(FirestoreDb firestore, IMapper mapper, IUserService userService)
{
    public CollectionReference PackagesReference { get; private set; } = firestore.Collection("packages");
    public CollectionReference PackagesLinks { get; private set; } = firestore.Collection("packages-links");

    public DocumentReference Document(string path) => firestore.Document(path);
    public CollectionReference Collecton(string path) => firestore.Collection(path);

    public async Task<PackageEntity?> Retrieve(string packageId, NuGetVersion packageVersion)
    {
        if (packageId == null) throw new ArgumentNullException(nameof(packageId));
        if (packageVersion == null) throw new ArgumentNullException(nameof(packageVersion));
        
        var packageRoot = await PackagesReference
            .Document(packageId)
            .GetSnapshotAsync();
        
        var result = packageVersion switch
        {
            { Metadata: "next" } when packageRoot.ContainsField("next") => await packageRoot
                .GetValue<DocumentReference>("next")
                .GetSnapshotAsync(),
            { Metadata: "next" } when !packageRoot.ContainsField("next") => await packageRoot
                .GetValue<DocumentReference>("latest")
                .GetSnapshotAsync(),
            { Metadata: "latest" } => await packageRoot
                .GetValue<DocumentReference>("latest")
                .GetSnapshotAsync(),
            { } => await PackagesReference
                .Document(packageId)
                .Collection("v")
                .Document(packageVersion.ToNormalizedString())
                .GetSnapshotAsync(),
            null => null
        };
        return result?.ConvertTo<PackageEntity>();
    }

    /// <summary>
    /// Add package into db.
    /// </summary>
    /// <param name="package">A package.</param>
    /// <param name="owner">A owner of package.</param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="OwnerIsNotMatchException">Owner UID is not matched.</exception>
    public async Task<WriteResult> AddPackage(Package package, UserRecord owner)
    {
        if (package == null) throw new ArgumentNullException(nameof(package));
        if (owner == null) throw new ArgumentNullException(nameof(owner));

        var version = package.Version;
        var normalizedVersion = version.ToNormalizedString();

        var entity = mapper.Map<PackageEntity>(package);

        var document = PackagesReference
            .Document(entity.Id);

        var snapshot = await document
            .GetSnapshotAsync();

        var verified = false;
        var isServiced = false;
        

        if (!snapshot.Exists)
        {
            if (!await userService.UserAllowedSkipPublishVerification())
                PackageValidator.ValidateNewPackage(package);

            var kv = new Dictionary<string, object>
            {
                { "owner", owner.Uid }
            };
            await document.CreateAsync(kv);
        }
        else
        {
            if (!snapshot.GetValue<string>("owner").Equals(owner.Uid, StringComparison.InvariantCultureIgnoreCase))
                throw new OwnerIsNotMatchException();
            if (snapshot.ContainsField("IsVerified"))
                verified = snapshot.GetValue<bool>("IsVerified");
            if (snapshot.ContainsField("IsServiced"))
                isServiced = snapshot.GetValue<bool>("IsServiced");
        }


        entity.IsVerified = verified;
        entity.HasServicedPackage = isServiced;

        var result = await PackagesReference
            .Document(entity.Id)
            .Collection("v")
            .Document(normalizedVersion)
            .CreateAsync(entity);


        var versions = await PackagesReference
            .Document(entity.Id)
            .Collection("v")
            .ListDocumentsAsync()
            .Where(x => NuGetVersion.TryParse(x.Id, out _))
            .Select(x => ((DocumentReference path, NuGetVersion version))(x, NuGetVersion.Parse(x.Id)))
            .ToListAsync();

        var latestVersion = versions.OrderByDescending(x => x.version).First(x => !x.version.IsPrerelease);
        var nextVersion = versions.OrderByDescending(x => x.version).FirstOrDefault(x => x.version.IsPrerelease);

        nextVersion = nextVersion.version >= latestVersion.version ? nextVersion : latestVersion;

        {
            var kv = new Dictionary<string, object>
            {
                { "latest", latestVersion.path },
                { "next", nextVersion.path }
            };

            await document.UpdateAsync(kv);
        }
        return result;
    }

    public async Task<bool> ExistAsync(string packageId, NuGetVersion? packageVersion = null, CancellationToken cancellationToken = default)
    {
        if (packageVersion is null)
        {
            var result = await PackagesReference
                    .Document(packageId).GetSnapshotAsync(cancellationToken);

            return result is not null && result.Exists;
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

        var snapshot = await PackagesReference.Document(packageId).GetSnapshotAsync();

        var totalDownloads = snapshot.ContainsField("TotalDownloads") ? snapshot.GetValue<ulong>("TotalDownloads") : 0ul;
        
        var kv = new Dictionary<string, object>
        {
            { "TotalDownloads", totalDownloads + 1 }
        };

        await PackagesReference
            .Document(packageId)
            .SetAsync(kv, SetOptions.MergeAll);

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
}

public class FirestoreSearchService(FireOperationBuilder operationBuilder, IUrlGenerator url, IMapper mapper)
    : ISearchService
{
    private static readonly Task<DependentsResponse> EmptyDependentsResponseTask =
            Task.FromResult(new DependentsResponse
            {
                TotalHits = 0,
                Data = new List<DependentResult>()
            });

    private readonly FireOperationBuilder _table = operationBuilder ?? throw new ArgumentNullException(nameof(operationBuilder));

    public async Task<IReadOnlyList<Package>> SearchAsync(
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

        return results.Select(ToSearchResult).ToList().AsReadOnly();
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
            return [];

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
                results.Add([]);
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

    private Package ToSearchResult(IReadOnlyList<PackageEntity> packages)
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
                ? url.GetPackageIconDownloadUrl(latest.Id, latestVersion)
                : latest.IconUrl;

        var result = mapper.Map<Package>(latest);
        result.Icon = iconUrl;
        return result;
    }
}
