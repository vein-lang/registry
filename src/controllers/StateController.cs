namespace core.controllers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using services;
using services.searchs;

[ApiController]
public class StateController : Controller
{
    private readonly IPackageService _packageService;
    private readonly IMemoryCache _cache;
    public StateController(IPackageService packageService, IMemoryCache cache)
    {
        _packageService = packageService;
        _cache = cache;
    }

    [HttpGet("@/state")]
    public async Task<IActionResult> GetState()
    {
        if (_cache.TryGetValue("@/state", out PackagesState state))
            return Json(state);
        var latest = await _packageService.GetLatestPackagesAsync();
        var count = await _packageService.GetPackagesCountAsync();
        var downloads = await _packageService.GetTotalDownloadsAsync();
        var popular = await _packageService.GetPopularPackagesAsync();


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

        _cache.Set("@/state", result, TimeSpan.FromHours(12));

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
