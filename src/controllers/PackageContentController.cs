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

    public PackageContentController(IPackageContentService content)
        => _content = content;

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
        if (!NuGetVersion.TryParse(version, out var nugetVersion))
            return NotFound();

        var packageStream = await _content.GetPackageContentStreamOrNullAsync(id, nugetVersion, cancellationToken);
        if (packageStream == null)
            return NotFound();

        return File(packageStream, "application/octet-stream");
    }

    [HttpGet("@/packages/{id}/{version}/spec.json")]
    public async Task<IActionResult> DownloadVeinSpecAsync(string id, string version, CancellationToken cancellationToken)
    {
        if (!NuGetVersion.TryParse(version, out var nugetVersion))
            return NotFound();

        var nuspecStream = await _content.GetPackageManifestStreamOrNullAsync(id, nugetVersion, cancellationToken);
        if (nuspecStream == null)
            return NotFound();

        return File(nuspecStream, "text/json");
    }

    [HttpGet("@/packages/{id}/{version}/readme")]
    public async Task<IActionResult> DownloadReadmeAsync(string id, string version, CancellationToken cancellationToken)
    {
        if (!NuGetVersion.TryParse(version, out var nugetVersion))
            return NotFound();

        var readmeStream = await _content.GetPackageReadmeStreamOrNullAsync(id, nugetVersion, cancellationToken);
        if (readmeStream == null)
            return NotFound();

        return File(readmeStream, "text/markdown");
    }

    [HttpGet("@/packages/{id}/{version}/icon")]
    public async Task<IActionResult> DownloadIconAsync(string id, string version, CancellationToken cancellationToken)
    {
        if (!NuGetVersion.TryParse(version, out var nugetVersion))
            return NotFound();

        var iconStream = await _content.GetPackageIconStreamOrNullAsync(id, nugetVersion, cancellationToken);
        if (iconStream == null)
            return NotFound();
        return File(iconStream, "image/png");
    }
}
