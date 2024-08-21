namespace core.controllers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using services;
using services.searchs;

[ApiController]
public class StateController(IPackageService packageService, IMemoryCache cache) : Controller
{
    [HttpGet("@/state")]
    public async Task<IActionResult> GetState()
    {
        if (cache.TryGetValue("@/state", out PackagesState state))
            return Json(state);
        var latest = await packageService.GetLatestPackagesAsync();
        var count = await packageService.GetPackagesCountAsync();
        var downloads = await packageService.GetTotalDownloadsAsync();
        var popular = await packageService.GetPopularPackagesAsync();


        var result = new PackagesState()
        {
            latest_packages = latest,
            packages_state =
            [
                new("Package downloads", downloads),
                new("Package total", count)
            ],
            popular_packages = popular
        };

        cache.Set("@/state", result, TimeSpan.FromHours(24));

        return Json(result);
    }
}

public record AnalyticsKeyValue(string key, ulong value);
public class PackagesState
{
    public IReadOnlyCollection<string> popular_packages { get; set; } 
    public IReadOnlyCollection<string> latest_packages { get; set; } 
    public List<AnalyticsKeyValue> packages_state { get; set; } = [];
}
