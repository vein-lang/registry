namespace core.services.searchs;

using vein.project;

/// <summary>
/// A no-op indexer, used when search does not need to index packages.
/// </summary>
public class NullSearchIndexer : ISearchIndexer
{
    public Task IndexAsync(PackageManifest package, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}



public interface ISearchIndexer
{
    /// <summary>
    /// Add a package to the search index.
    /// </summary>
    /// <param name="package">The package to add.</param>
    /// <param name="cancellationToken">A token to cancel the task.</param>
    /// <returns>A task that completes once the package has been added.</returns>
    Task IndexAsync(PackageManifest package, CancellationToken cancellationToken);
}
