namespace core.controllers
{
    using core.services.searchs;
    using core.services.searchs.models;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Caching.Memory;
    using NuGet.Versioning;
    using services;

    [AllowAnonymous]
    [ApiController]
    public class SearchController : Controller
    {
        private readonly ISearchService _searchService;
        private readonly IPackageService _packageService;
        private readonly IUrlGenerator _url;
        private readonly IMemoryCache _cache;


        public SearchController(ISearchService searchService, IPackageService packageService, IUrlGenerator urlGenerator, IMemoryCache cache)
        {
            _searchService = searchService;
            _packageService = packageService;
            _url = urlGenerator;
            _cache = cache;
        }

        [HttpGet("@/package/{name}/{version}")]
        public async Task<ActionResult<Package>> FindByName(string name, string version, [FromQuery] bool includeUnlisted = false)
        {
            if (_cache.TryGetValue((name, version), out Package package))
                return Json(package);

            var ver = version switch
            {
                "latest" or null => new (0, 0, 0, 0, "", "latest"),
                "next"           => new (0, 0, 0, 0, "", "next"),
                not null         => NuGetVersion.Parse(version)
            };

            var result = await _packageService.FindOrNullAsync(name, ver, includeUnlisted);

            if (result == null)
                return StatusCode(404);

            result.Icon = result.HasEmbeddedIcon
                ? _url.GetPackageIconDownloadUrl(result.Name, result.Version)
                : result.Icon;


            _cache.Set((name, version), result, ver.HasMetadata ? TimeSpan.FromMinutes(15) : TimeSpan.FromDays(2));

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

            return Json(await _searchService.SearchAsync(request, cancellationToken));
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

                return await _searchService.ListPackageVersionsAsync(request, cancellationToken);
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

                return await _searchService.AutocompleteAsync(request, cancellationToken);
            }
        }

        [HttpGet("@/search/dependents")]
        public async Task<ActionResult<DependentsResponse>> DependentsAsync(
            [FromQuery] string packageId = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(packageId))
                return BadRequest();
            return await _searchService.FindDependentsAsync(packageId, cancellationToken);
        }
    }
}
