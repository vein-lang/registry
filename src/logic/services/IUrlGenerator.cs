namespace core.services;

using NuGet.Versioning;


public class RegistryUrlGenerator : IUrlGenerator
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    public RegistryUrlGenerator(IHttpContextAccessor httpContextAccessor)
        => _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));

    public string? GetServiceIndexUrl() =>
        AbsoluteUrl("@/index.json");

    public string GetPackageContentResourceUrl()
        => AbsoluteUrl("@/package");

    public string? GetPackagePublishResourceUrl() =>
        AbsoluteUrl("@/publish");

    public string? GetSymbolPublishResourceUrl() =>
        AbsoluteUrl("@/publish/symbols");

    public string? GetSearchResourceUrl() =>
        AbsoluteUrl("@/search/index");

    public string? GetAutocompleteResourceUrl() =>
        AbsoluteUrl("@/search/lint");

    public string? GetPackageVersionsUrl(string id)
    => AbsoluteUrl($"@/packages/{id}/version.json");

    public string? GetPackageDownloadUrl(string id, NuGetVersion version)
        => AbsoluteUrl($"@/packages/{id}/{version}");

    public string? GetPackageManifestDownloadUrl(string id, NuGetVersion version)
        => AbsoluteUrl($"@/packages/{id}/{version}/spec.json");

    public string? GetPackageIconDownloadUrl(string id, NuGetVersion version)
        => AbsoluteUrl($"@/packages/{id}/{version}/icon");

    public string? GetPackageReadmeDownloadUrl(string id, NuGetVersion version)
        => AbsoluteUrl($"@/packages/{id}/{version}/readme");

    private string AbsoluteUrl(string relativePath)
    {
        var request = _httpContextAccessor.HttpContext!.Request;

        return string.Concat(
            request.Scheme,
            "://",
            request.Host.ToUriComponent(),
            request.PathBase.ToUriComponent(),
            "/",
            relativePath);
    }
}

/// <summary>
/// Used to create URLs to resources in the NuGet protocol.
/// </summary>
public interface IUrlGenerator
{
    /// <summary>
    /// Get the URL for the package source (also known as the "service index").
    /// </summary>
    string? GetServiceIndexUrl();

    /// <summary>
    /// Get the URL for the root of the package content resource.
    /// </summary>
    string? GetPackageContentResourceUrl();
    
    /// <summary>
    /// Get the URL to publish packages.
    /// </summary>
    string? GetPackagePublishResourceUrl();

    /// <summary>
    /// Get the URL to publish symbol packages.
    /// </summary>
    string? GetSymbolPublishResourceUrl();

    /// <summary>
    /// Get the URL to search for packages.
    /// </summary>
    string? GetSearchResourceUrl();

    /// <summary>
    /// Get the URL to autocomplete package IDs.
    /// </summary>
    string? GetAutocompleteResourceUrl();
    
    /// <summary>
    /// Get the URL that lists a package's versions.
    /// </summary>
    /// <param name="id">The package's ID</param>
    string? GetPackageVersionsUrl(string id);

    /// <summary>
    /// Get the URL to download a package (.nupkg).
    /// </summary>
    /// <param name="id">The package's ID</param>
    /// <param name="version">The package's version</param>
    string? GetPackageDownloadUrl(string id, NuGetVersion version);

    /// <summary>
    /// Get the URL to download a package's manifest (.nuspec).
    /// </summary>
    /// <param name="id">The package's ID</param>
    /// <param name="version">The package's version</param>
    string? GetPackageManifestDownloadUrl(string id, NuGetVersion version);

    /// <summary>
    /// Get the URL to download a package icon.
    /// </summary>
    /// <param name="id">The package's ID</param>
    /// <param name="version">The package's version</param>
    string? GetPackageIconDownloadUrl(string id, NuGetVersion version);

    /// <summary>
    /// Get the URL to download a package readme.
    /// </summary>
    /// <param name="id">The package's ID</param>
    /// <param name="version">The package's version</param>
    string? GetPackageReadmeDownloadUrl(string id, NuGetVersion version);
}
