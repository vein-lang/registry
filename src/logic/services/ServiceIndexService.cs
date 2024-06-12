namespace core.services;

using core.controllers;

/// <summary>
/// The NuGet Service Index service, used to discover other resources.
/// </summary>
public interface IServiceIndexService
{
    /// <summary>
    /// Get the resources available on this package feed.
    /// </summary>
    /// <returns>The resources available on this package feed.</returns>
    Task<ServiceIndexResponse> GetAsync(CancellationToken cancellationToken = default);
}


public class RegistryServiceIndex : IServiceIndexService
{
    private readonly IUrlGenerator _url;

    public RegistryServiceIndex(IUrlGenerator url)
        => _url = url ?? throw new ArgumentNullException(nameof(url));

    private ServiceIndexItem BuildResource(string name, string url) =>
        new()
        {
            ResourceUrl = url,
            Type = name,
        };

    public Task<ServiceIndexResponse> GetAsync(CancellationToken cancellationToken = default)
    {
        var resources = new List<ServiceIndexItem>();
        resources.Add(BuildResource("PackagePublish", _url.GetPackagePublishResourceUrl()));
        resources.Add(BuildResource("SymbolPackagePublish", _url.GetSymbolPublishResourceUrl()));
        resources.Add(BuildResource("SearchQueryService", _url.GetSearchResourceUrl()));
        resources.Add(BuildResource("PackageBaseAddress", _url.GetPackageContentResourceUrl()));
        resources.Add(BuildResource("SearchAutocompleteService", _url.GetAutocompleteResourceUrl()));

        var result = new ServiceIndexResponse
        {
            Resources = resources,
        };

        return Task.FromResult(result);
    }
}
