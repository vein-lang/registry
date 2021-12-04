namespace core;

public static class HttpRequestExtensions
{
    public const string ApiKeyHeader = "X-VEIN-API-KEY";

    public static async Task<Stream?> GetUploadStreamOrNullAsync(this HttpRequest request, CancellationToken cancellationToken)
    {
        Stream? rawUploadStream = null;
        try
        {
            if (request.HasFormContentType && request.Form.Files.Count > 0)
            {
                rawUploadStream = request.Form.Files[0].OpenReadStream();
            }
            else
            {
                rawUploadStream = request.Body;
            }

            // Convert the upload stream into a temporary file stream to
            // minimize memory usage.
            return await rawUploadStream?.AsTemporaryFileStreamAsync(cancellationToken);
        }
        finally
        {
            rawUploadStream?.Dispose();
        }
    }

    public static string GetApiKey(this HttpRequest request)
        => request.Headers[ApiKeyHeader];
}
