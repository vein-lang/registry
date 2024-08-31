namespace core.controllers;

using services.searchs.models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using NuGet.Versioning;
using services;
using services.searchs;

public class PackageCacheSystem(IMemoryCache cache)
{
    public bool TryGetPackage(string name, string version, out Package? pkg)
        => cache.TryGetValue($"packages/{name}/{version}", out pkg);

    public void SetPackage(string name, string version, Package? pkg)
        => cache.Set($"packages/{name}/{version}", pkg, TimeSpan.FromDays(30));

    public void InvalidatePackage(string name, string version)
    {
        if (TryGetPackage(name, version, out _))
            cache.Remove($"packages/{name}/{version}");
    }

    public bool TryGetSearchResult(SearchRequest request, out IReadOnlyList<Package> result)
    {
        var r = cache.TryGetValue<IReadOnlyList<Package>>(request, out var list);

        result = list ?? new List<Package>();

        return r;
    }

    public void SetSearchResult(SearchRequest request, IReadOnlyList<Package> result)
        => cache.Set(request, result, new MemoryCacheEntryOptions()
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1),
            Priority = CacheItemPriority.Low,
            Size = 1024
        });
}

[AllowAnonymous]
[ApiController]
public class SearchController(
    ISearchService searchService,
    IPackageService packageService,
    IUrlGenerator urlGenerator,
    PackageCacheSystem cacheSystem)
    : Controller
{
    [HttpGet("@/package/{name}/{version}")]
    public async Task<ActionResult<Package>> FindByName(string name, string version, [FromQuery] bool includeUnlisted = false)
    {
        if (string.IsNullOrEmpty(version))
            return BadRequest(new { message = "version cannot be null" });

        if (cacheSystem.TryGetPackage(name, version, out var package))
            return Json(package);
        
        var ver = version switch
        {
            Package.LatestTag or null => new (0, 0, 0, 0, "", Package.LatestTag),
            Package.NextTag           => new (0, 0, 0, 0, "", Package.NextTag),
            not null         => NuGetVersion.Parse(version)
        };

        var result = await packageService.FindOrNullAsync(name, ver, includeUnlisted);

        if (result == null)
            return StatusCode(404);

        result.Icon = result.HasEmbeddedIcon
            ? urlGenerator.GetPackageIconDownloadUrl(result.Name, result.Version)
            : result.Icon;

        cacheSystem.SetPackage(name, version!, result);
        return Json(result);
    }

    [HttpGet("@/search/index")]
    public async Task<ActionResult<IReadOnlyList<Package>>> SearchAsync(
        [FromQuery(Name = "q")] string? query = null,
        [FromQuery]int skip = 0,
        [FromQuery]int take = 20,
        [FromQuery]bool prerelease = false,
        [FromQuery]string? packageType = null,
        [FromQuery]string? framework = null,
        CancellationToken cancellationToken = default)
    {
        var request = new SearchRequest
        {
            Skip = skip,
            Take = take,
            IncludePrerelease = prerelease,
            IncludeSemVer2 = true,
            PackageType = packageType,
            Framework = framework,
            Query = query ?? string.Empty,
        };

        if (cacheSystem.TryGetSearchResult(request, out var result))
            return Json(result);

        result = await searchService.SearchAsync(request, cancellationToken);

        cacheSystem.SetSearchResult(request, result);

        return Json(result);
    }

    [HttpGet("@/search/lint")]
    public async Task<ActionResult<AutocompleteResponse>> AutocompleteAsync(
        [FromQuery(Name = "q")] string? autocompleteQuery = null,
        [FromQuery(Name = "id")] string? versionsQuery = null,
        [FromQuery]bool prerelease = false,
        [FromQuery]int skip = 0,
        [FromQuery]int take = 20,
        [FromQuery]string packageType = null,
        CancellationToken cancellationToken = default)
    {
        // If only "id" is provided, find package versions. Otherwise, find package IDs.
        if (versionsQuery != null && autocompleteQuery == null)
        {
            var request = new VersionsRequest
            {
                IncludePrerelease = prerelease,
                IncludeSemVer2 = true,
                PackageId = versionsQuery,
            };

            return await searchService.ListPackageVersionsAsync(request, cancellationToken);
        }
        else
        {
            var request = new AutocompleteRequest
            {
                IncludePrerelease = prerelease,
                IncludeSemVer2 = true,
                PackageType = packageType,
                Skip = skip,
                Take = take,
                Query = autocompleteQuery ?? "",
            };

            return await searchService.AutocompleteAsync(request, cancellationToken);
        }
    }

    [HttpGet("@/search/dependents")]
    public async Task<ActionResult<DependentsResponse>> DependentsAsync(
        [FromQuery] string? packageId = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(packageId))
            return BadRequest();
        return await searchService.FindDependentsAsync(packageId, cancellationToken);
    }
}
