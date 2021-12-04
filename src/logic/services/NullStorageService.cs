namespace core.services;

/// <summary>
/// A minimal storage implementation, used for advanced scenarios.
/// </summary>
public class NullStorageService : IStorageService
{
    public Task DeleteAsync(string path, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task<Stream> GetAsync(string path, CancellationToken cancellationToken = default)
        => Task.FromResult<Stream>(null);

    public Task<Uri> GetDownloadUriAsync(string path, CancellationToken cancellationToken = default)
        => Task.FromResult<Uri>(null);

    public Task<StoragePutResult> PutAsync(
        string path,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(StoragePutResult.Success);
}
