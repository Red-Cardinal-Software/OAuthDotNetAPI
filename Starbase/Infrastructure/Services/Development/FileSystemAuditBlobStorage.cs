using System.Security.Cryptography;
using Application.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.Development;

/// <summary>
/// Development audit blob storage that writes to the local file system.
/// Useful for local development and testing without requiring cloud storage.
/// </summary>
/// <remarks>
/// This service is intended for development use only. In production, replace with
/// an immutable cloud storage implementation (Azure Blob with WORM, AWS S3 with Object Lock, etc.).
///
/// Files are stored in: {AppData}/starbase/audit-archives/{year}/{month}/
///
/// WARNING: Local file system storage does NOT provide:
/// - Immutability guarantees
/// - Compliance-grade audit trails
/// - High availability
/// - Disaster recovery
///
/// Always use cloud storage with immutable policies in production.
/// </remarks>
public class FileSystemAuditBlobStorage : IAuditBlobStorage
{
    private readonly ILogger<FileSystemAuditBlobStorage> _logger;
    private readonly string _basePath;

    public FileSystemAuditBlobStorage(ILogger<FileSystemAuditBlobStorage> logger)
    {
        _logger = logger;

        // Use a consistent path that works across platforms
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _basePath = Path.Combine(appData, "starbase", "audit-archives");

        Directory.CreateDirectory(_basePath);

        _logger.LogInformation(
            "FileSystemAuditBlobStorage initialized. Archives will be stored at: {BasePath}. " +
            "This is for DEVELOPMENT ONLY - use cloud storage in production.",
            _basePath);
    }

    public async Task<BlobUploadResult> UploadAsync(
        DateTime partitionBoundary,
        Stream data,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Create directory structure: basePath/year/month/
            var directory = Path.Combine(
                _basePath,
                partitionBoundary.Year.ToString(),
                partitionBoundary.Month.ToString("D2"));

            Directory.CreateDirectory(directory);

            // Generate filename with timestamp for uniqueness
            var filename = $"audit_{partitionBoundary:yyyy-MM-dd}_{DateTime.UtcNow:HHmmss}_{Guid.NewGuid().ToString()[..8]}.json";
            var filePath = Path.Combine(directory, filename);

            // Read all data to compute hash and write file
            using var memoryStream = new MemoryStream();
            await data.CopyToAsync(memoryStream, cancellationToken);
            var bytes = memoryStream.ToArray();

            // Compute SHA-256 hash
            var hashBytes = SHA256.HashData(bytes);
            var contentHash = Convert.ToHexString(hashBytes).ToLowerInvariant();

            // Write file
            await File.WriteAllBytesAsync(filePath, bytes, cancellationToken);

            // Use file:// URI scheme for local files
            var uri = new Uri(filePath).AbsoluteUri;

            _logger.LogInformation(
                "Audit archive written to local file system. Path: {FilePath}, Size: {SizeBytes} bytes, Hash: {Hash}",
                filePath, bytes.Length, contentHash[..16] + "...");

            return BlobUploadResult.Succeeded(uri, contentHash, bytes.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write audit archive to file system");
            return BlobUploadResult.Failed($"File system write failed: {ex.Message}");
        }
    }

    public async Task<Stream> DownloadAsync(
        string uri,
        CancellationToken cancellationToken = default)
    {
        var filePath = GetFilePathFromUri(uri);

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Audit archive not found: {filePath}");
        }

        var bytes = await File.ReadAllBytesAsync(filePath, cancellationToken);
        return new MemoryStream(bytes);
    }

    public async Task<string> GetBlobHashAsync(
        string uri,
        CancellationToken cancellationToken = default)
    {
        var filePath = GetFilePathFromUri(uri);

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Audit archive not found: {filePath}");
        }

        var bytes = await File.ReadAllBytesAsync(filePath, cancellationToken);
        var hashBytes = SHA256.HashData(bytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    public Task<bool> ExistsAsync(
        string uri,
        CancellationToken cancellationToken = default)
    {
        var filePath = GetFilePathFromUri(uri);
        return Task.FromResult(File.Exists(filePath));
    }

    private static string GetFilePathFromUri(string uri)
    {
        // Handle both file:// URIs and raw paths
        if (uri.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
        {
            return new Uri(uri).LocalPath;
        }

        return uri;
    }
}