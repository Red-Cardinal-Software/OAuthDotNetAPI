namespace Application.Interfaces.Services;

/// <summary>
/// Abstraction for immutable blob storage used for audit archive.
/// Implementations should use immutable/WORM storage for compliance.
/// </summary>
public interface IAuditBlobStorage
{
    /// <summary>
    /// Uploads audit data to immutable blob storage.
    /// </summary>
    /// <param name="partitionBoundary">The partition date (used for path/naming)</param>
    /// <param name="data">The audit data stream to upload</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result with URI and content hash</returns>
    Task<BlobUploadResult> UploadAsync(
        DateTime partitionBoundary,
        Stream data,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads archived audit data for verification or restore.
    /// </summary>
    /// <param name="uri">The blob URI from the archive manifest</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Stream containing the archived data</returns>
    Task<Stream> DownloadAsync(
        string uri,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Computes the hash of a blob for integrity verification.
    /// </summary>
    /// <param name="uri">The blob URI to verify</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>SHA-256 hash of the blob content</returns>
    Task<string> GetBlobHashAsync(
        string uri,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a blob exists at the given URI.
    /// </summary>
    /// <param name="uri">The blob URI to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if blob exists</returns>
    Task<bool> ExistsAsync(
        string uri,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of uploading audit data to blob storage.
/// </summary>
public record BlobUploadResult
{
    /// <summary>
    /// Whether the upload succeeded.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// The URI where the blob was stored.
    /// </summary>
    public string? Uri { get; init; }

    /// <summary>
    /// SHA-256 hash of the uploaded content.
    /// </summary>
    public string? ContentHash { get; init; }

    /// <summary>
    /// Size of the uploaded blob in bytes.
    /// </summary>
    public long SizeBytes { get; init; }

    /// <summary>
    /// Error message if upload failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    public static BlobUploadResult Succeeded(string uri, string contentHash, long sizeBytes)
        => new() { Success = true, Uri = uri, ContentHash = contentHash, SizeBytes = sizeBytes };

    public static BlobUploadResult Failed(string errorMessage)
        => new() { Success = false, ErrorMessage = errorMessage };
}