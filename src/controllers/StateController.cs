namespace core.controllers;

using Microsoft.AspNetCore.Mvc;
using services;
using services.searchs;

[ApiController]
public class StateController : Controller
{
    private readonly FireOperationBuilder _builder;
    private readonly IPackageService _packageService;

    public StateController(FireOperationBuilder builder, IPackageService packageService)
    {
        _builder = builder;
        _packageService = packageService;
    }

    [HttpGet("@/state")]
    public async Task<IActionResult> GetState()
    {
        var latest = await _packageService.GetLatestPackagesAsync();
        var count = await _packageService.GetPackagesCountAsync();
        var downloads = await _packageService.GetTotalDownloadsAsync();
        var popular = await _packageService.GetPopularPackagesAsync();

        return Json(new PackagesState()
        {
            latest_packages = latest,
            packages_state = new List<AnalyticsKeyValue>()
            {
                { new ("Package downloads", downloads) },
                { new ("Package total", count) },
            },
            popular_packages = popular
        });
    }
}

public record AnalyticsKeyValue(string key, ulong value);
public class PackagesState
{
    public IReadOnlyCollection<string> popular_packages { get; set; } 
    public IReadOnlyCollection<string> latest_packages { get; set; } 
    public List<AnalyticsKeyValue> packages_state { get; set; } = new ();
}
