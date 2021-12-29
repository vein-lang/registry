namespace core.controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NuGet.Versioning;
using services;

[AllowAnonymous]
[ApiController]
public class PackageContentController : Controller
{
    private readonly IPackageContentService _content;
    private readonly MarkdownService _markdownService;

    public PackageContentController(IPackageContentService content, MarkdownService markdownService)
        => (_content, _markdownService) = (content, markdownService);

    [HttpGet("@/packages/{id}/version.json")]
    public async Task<ActionResult<PackageVersionsResponse>> GetPackageVersionsAsync(string id, CancellationToken cancellationToken)
    {
        var versions = await _content.GetPackageVersionsOrNullAsync(id, cancellationToken);
        if (versions == null)
            return NotFound();
        return versions;
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

        var result = await reader.ReadToEndAsync();
        
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
