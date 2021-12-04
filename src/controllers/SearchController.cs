namespace core.controllers
{
    using core.services.searchs;
    using core.services.searchs.models;
    using Microsoft.AspNetCore.Mvc;

    public class SearchController : Controller
    {
        private readonly ISearchService _searchService;

        public SearchController(ISearchService searchService)
            => _searchService = searchService;

        [HttpGet("@/search/index")]
        public async Task<ActionResult<SearchResponse>> SearchAsync(
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

            return await _searchService.SearchAsync(request, cancellationToken);
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
