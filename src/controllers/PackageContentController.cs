namespace core.controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using NuGet.Versioning;
using services;

[AllowAnonymous]
[ApiController]
public class PackageContentController(
    IPackageContentService content,
    MarkdownService markdownService,
    IMemoryCache cache)
    : Controller
{
    [HttpGet("@/packages/{id}/version.json")]
    public async Task<ActionResult<PackageVersionsResponse>> GetPackageVersionsAsync(string id, CancellationToken cancellationToken)
    {
        if (cache.TryGetValue($"@/packages/{id}/version.json", out PackageVersionsResponse result))
            return Json(result);

        var versions = await content.GetPackageVersionsOrNullAsync(id, cancellationToken);
        if (versions == null)
            return NotFound();

        cache.Set($"@/packages/{id}/version.json", versions, TimeSpan.FromMinutes(5));

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

        var packageStream = await content.GetPackageContentStreamOrNullAsync(id, ver, cancellationToken);
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

        var nuspecStream = await content.GetPackageManifestStreamOrNullAsync(id, ver, cancellationToken);
        if (nuspecStream == null)
            return NotFound();

        return File(nuspecStream, "text/json");
    }

    [HttpGet("@/packages/{id}/{version}/readme")]
    public async Task<IActionResult> DownloadReadmeAsync(string id, string version, CancellationToken cancellationToken)
    {
        if (cache.TryGetValue($"@/packages/{id}/{version}/readme", out string md))
            return Content(markdownService.GetHtmlFromMarkdown(md).Content, "text/html");
        var ver = version switch
        {
            "latest" or null => new (0, 0, 0, 0, "", "latest"),
            "next"           => new (0, 0, 0, 0, "", "next"),
            not null         => NuGetVersion.Parse(version)
        };

        var readmeStream = await content.GetPackageReadmeStreamOrNullAsync(id, ver, cancellationToken);

        if (readmeStream == null)
            return NotFound();

        using var reader = new StreamReader(readmeStream);

        var result = await reader.ReadToEndAsync(cancellationToken);

        cache.Set($"@/packages/{id}/{version}/readme", result, TimeSpan.FromHours(6));
        
        return Content(markdownService.GetHtmlFromMarkdown(result).Content, "text/html");
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

        var iconStream = await content.GetPackageIconStreamOrNullAsync(id, ver, cancellationToken);
        if (iconStream == null)
            return NotFound();
        return File(iconStream, "image/png");
    }
}
