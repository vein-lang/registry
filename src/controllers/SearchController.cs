namespace core.controllers;

using services.searchs.models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using NuGet.Versioning;
using services;
using services.searchs;

[AllowAnonymous]
[ApiController]
public class SearchController(
    ISearchService searchService,
    IPackageService packageService,
    IUrlGenerator urlGenerator,
    IMemoryCache cache)
    : Controller
{
    [HttpGet("@/package/{name}/{version}")]
    public async Task<ActionResult<Package>> FindByName(string name, string version, [FromQuery] bool includeUnlisted = false)
    {
        if (string.IsNullOrEmpty(version))
            return BadRequest(new { message = "version cannot be null" });

        if (cache.TryGetValue((name, version), out Package? package))
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

        if (!version!.Equals(Package.LatestTag) && !version.Equals(Package.NextTag))
            return Json(result);

        cache.Set((name, version), result, TimeSpan.FromDays(2));
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

        return Json(await searchService.SearchAsync(request, cancellationToken));
    }

    [HttpGet("@/search/lint")]
    public async Task<ActionResult<AutocompleteResponse>> AutocompleteAsync(
        [FromQuery(Name = "q")] string autocompleteQuery = null,
        [FromQuery(Name = "id")] string versionsQuery = null,
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
                Query = autocompleteQuery,
            };

            return await searchService.AutocompleteAsync(request, cancellationToken);
        }
    }

    [HttpGet("@/search/dependents")]
    public async Task<ActionResult<DependentsResponse>> DependentsAsync(
        [FromQuery] string packageId = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(packageId))
            return BadRequest();
        return await searchService.FindDependentsAsync(packageId, cancellationToken);
    }
}
