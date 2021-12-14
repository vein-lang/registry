namespace core.services.searchs;

using core.services.searchs.models;

/// <summary>
/// A minimal search service implementation, used for advanced scenarios.
/// </summary>
public class NullSearchService : ISearchService
{
    private static readonly IReadOnlyList<string> EmptyStringList = new List<string>();

    private static readonly Task<AutocompleteResponse> EmptyAutocompleteResponseTask =
        Task.FromResult(new AutocompleteResponse
        {
            TotalHits = 0,
            Data = EmptyStringList,
            Context = AutocompleteContext.Default
        });

    private static readonly Task<DependentsResponse> EmptyDependentsResponseTask =
        Task.FromResult(new DependentsResponse
        {
            TotalHits = 0,
            Data = new List<DependentResult>()
        });
    
    public Task<AutocompleteResponse> AutocompleteAsync(
        AutocompleteRequest request,
        CancellationToken cancellationToken) =>
        EmptyAutocompleteResponseTask;

    public Task<AutocompleteResponse> ListPackageVersionsAsync(
        VersionsRequest request,
        CancellationToken cancellationToken) =>
        EmptyAutocompleteResponseTask;

    public Task<DependentsResponse> FindDependentsAsync(string packageId, CancellationToken cancellationToken) => EmptyDependentsResponseTask;

    public async Task<IReadOnlyList<Package>> SearchAsync(
        SearchRequest request,
        CancellationToken cancellationToken) =>
        new List<Package>().AsReadOnly();
}
