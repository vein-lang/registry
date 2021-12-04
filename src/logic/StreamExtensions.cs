namespace core;

using System.Security.Cryptography;

public static class StreamExtensions
{
    // See: https://github.com/dotnet/corefx/blob/master/src/Common/src/CoreLib/System/IO/Stream.cs#L35
    private const int DefaultCopyBufferSize = 81920;

    /// <summary>
    /// Copies a stream to a file, and returns that file as a stream. The underlying file will be
    /// deleted when the resulting stream is disposed.
    /// </summary>
    /// <param name="original">The stream to be copied, at its current position.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>The copied stream, with its position reset to the beginning.</returns>
    public static async Task<FileStream> AsTemporaryFileStreamAsync(
        this Stream original,
        CancellationToken cancellationToken = default)
    {
        var result = new FileStream(
            Path.GetTempFileName(),
            FileMode.Create,
            FileAccess.ReadWrite,
            FileShare.None,
            DefaultCopyBufferSize,
            FileOptions.DeleteOnClose);
        
        try
        {
            await CopyToAsync(original, -1, result, DefaultCopyBufferSize,
                new Progress<KeyValuePair<long, long>>((x) => Console.WriteLine($"{x}")), cancellationToken);
            //await original.CopyToAsync(result, cancellationToken);
            result.Position = 0;
        }
        catch (Exception)
        {
            result.Dispose();
            throw;
        }

        return result;
    }


    /// <summary>
    /// Copys a stream to another stream
    /// </summary>
    /// <param name="source">The source <see cref="Stream"/> to copy from</param>
    /// <param name="sourceLength">The length of the source stream, 
    /// if known - used for progress reporting</param>
    /// <param name="destination">The destination <see cref="Stream"/> to copy to</param>
    /// <param name="bufferSize">The size of the copy block buffer</param>
    /// <param name="progress">An <see cref="IProgress{T}"/> implementation 
    /// for reporting progress</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A task representing the operation</returns>
    public static async Task CopyToAsync(
        Stream source, 
        long sourceLength,
        Stream destination, 
        int bufferSize, 
        IProgress<KeyValuePair<long,long>> progress, 
        CancellationToken cancellationToken)
    {
        if (0 == bufferSize)
            bufferSize = DefaultCopyBufferSize;
        var buffer = new byte[bufferSize];
        if(0>sourceLength && source.CanSeek)
            sourceLength = source.Length - source.Position;
        var totalBytesCopied = 0L;
        if (null != progress)
            progress.Report(new KeyValuePair<long, long>(totalBytesCopied, sourceLength));
        var bytesRead = -1;
        while(0!=bytesRead && !cancellationToken.IsCancellationRequested)
        {
            bytesRead = await source.ReadAsync(buffer, 0, buffer.Length);
            if (0 == bytesRead || cancellationToken.IsCancellationRequested)
                break;
            await destination.WriteAsync(buffer, 0, buffer.Length);
            totalBytesCopied += bytesRead;
            if (null != progress)
                progress.Report(new KeyValuePair<long, long>(totalBytesCopied, sourceLength));
        }
        if(0<totalBytesCopied)
            progress.Report(new KeyValuePair<long, long>(totalBytesCopied, sourceLength));
        cancellationToken.ThrowIfCancellationRequested();
    }
    public static bool Matches(this Stream content, Stream target)
    {
        using (var sha256 = SHA256.Create())
        {
            var contentHash = sha256.ComputeHash(content);
            var targetHash = sha256.ComputeHash(target);

            return contentHash.SequenceEqual(targetHash);
        }
    }
}
