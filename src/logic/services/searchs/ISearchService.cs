namespace core.services.searchs;

using core.services.searchs.models;

/// <summary>
/// The service used to search for packages.
/// 
/// See https://docs.microsoft.com/en-us/nuget/api/search-query-service-resource
/// </summary>
public interface ISearchService
{
    /// <summary>
    /// Perform a search query.
    /// </summary>
    /// <param name="request">The search request.</param>
    /// <param name="cancellationToken">A token to cancel the task.</param>
    /// <returns>The search response.</returns>
    Task<SearchResponse> SearchAsync(SearchRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Perform an autocomplete query.
    /// </summary>
    /// <param name="request">The autocomplete request.</param>
    /// <param name="cancellationToken">A token to cancel the task.</param>
    /// <returns>The autocomplete response.</returns>
    Task<AutocompleteResponse> AutocompleteAsync(AutocompleteRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Enumerate listed package versions.
    /// </summary>
    /// <param name="request">The autocomplete request.</param>
    /// <param name="cancellationToken">A token to cancel the task.</param>
    /// <returns>The package versions that matched the request.</returns>
    Task<AutocompleteResponse> ListPackageVersionsAsync(VersionsRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Find the packages that depend on a given package.
    /// </summary>
    /// <param name="packageId">The package whose dependents should be found.</param>
    /// <param name="cancellationToken">A token to cancel the task.</param>
    /// <returns>The dependents response.</returns>
    Task<DependentsResponse> FindDependentsAsync(
        string packageId,
        CancellationToken cancellationToken);
}



