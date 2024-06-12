namespace core.controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using NuGet.Versioning;
using services;

[AllowAnonymous]
[ApiController]
public class PackageContentController : Controller
{
    private readonly IPackageContentService _content;
    private readonly MarkdownService _markdownService;
    private readonly IMemoryCache _cache;

    public PackageContentController(IPackageContentService content, MarkdownService markdownService, IMemoryCache cache)
        => (_content, _markdownService, _cache) = (content, markdownService, cache);

    [HttpGet("@/packages/{id}/version.json")]
    public async Task<ActionResult<PackageVersionsResponse>> GetPackageVersionsAsync(string id, CancellationToken cancellationToken)
    {
        if (_cache.TryGetValue($"@/packages/{id}/version.json", out PackageVersionsResponse resut))
            return Json(resut);

        var versions = await _content.GetPackageVersionsOrNullAsync(id, cancellationToken);
        if (versions == null)
            return NotFound();

        _cache.Set($"@/packages/{id}/version.json", versions, TimeSpan.FromMinutes(15));

        return Json(versions);
    }

    [HttpGet("@/packages/{id}/{version}")]
    public async Task<IActionResult> DownloadPackageAsync(string id, string version, CancellationToken cancellationToken)
    {
        var ver = version switch
        {
            "latest" or null => new (0, 0, 0, 0, "", "latest"),
            "next"           => new (0, 0, 0, 0, "", "next"),
            not null         => NuGetVersion.Parse(version)
        };

        var packageStream = await _content.GetPackageContentStreamOrNullAsync(id, ver, cancellationToken);
        if (packageStream == null)
            return NotFound();

        return File(packageStream, "application/octet-stream");
    }

    [HttpGet("@/packages/{id}/{version}/spec.json")]
    public async Task<IActionResult> DownloadVeinSpecAsync(string id, string version, CancellationToken cancellationToken)
    {
        var ver = version switch
        {
            "latest" or null => new (0, 0, 0, 0, "", "latest"),
            "next"           => new (0, 0, 0, 0, "", "next"),
            not null         => NuGetVersion.Parse(version)
        };

        var nuspecStream = await _content.GetPackageManifestStreamOrNullAsync(id, ver, cancellationToken);
        if (nuspecStream == null)
            return NotFound();

        return File(nuspecStream, "text/json");
    }

    [HttpGet("@/packages/{id}/{version}/readme")]
    public async Task<IActionResult> DownloadReadmeAsync(string id, string version, CancellationToken cancellationToken)
    {
        if (_cache.TryGetValue($"@/packages/{id}/{version}/readme", out string md))
            return Content(_markdownService.GetHtmlFromMarkdown(md).Content, "text/html");
        var ver = version switch
        {
            "latest" or null => new (0, 0, 0, 0, "", "latest"),
            "next"           => new (0, 0, 0, 0, "", "next"),
            not null         => NuGetVersion.Parse(version)
        };

        var readmeStream = await _content.GetPackageReadmeStreamOrNullAsync(id, ver, cancellationToken);

        if (readmeStream == null)
            return NotFound();

        using var reader = new StreamReader(readmeStream);

        var result = await reader.ReadToEndAsync(cancellationToken);

        _cache.Set($"@/packages/{id}/{version}/readme", result,
            ver.HasMetadata ? TimeSpan.FromMinutes(15) : TimeSpan.FromHours(6));
        
        return Content(_markdownService.GetHtmlFromMarkdown(result).Content, "text/html");
    }

    [HttpGet("@/packages/{id}/{version}/icon")]
    public async Task<IActionResult> DownloadIconAsync(string id, string version, CancellationToken cancellationToken)
    {
        var ver = version switch
        {
            "latest" or null => new (0, 0, 0, 0, "", "latest"),
            "next"           => new (0, 0, 0, 0, "", "next"),
            not null         => NuGetVersion.Parse(version)
        };

        var iconStream = await _content.GetPackageIconStreamOrNullAsync(id, ver, cancellationToken);
        if (iconStream == null)
            return NotFound();
        return File(iconStream, "image/png");
    }
}
