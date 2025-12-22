using System.Collections.Concurrent;
using System.Security.Cryptography;
using Application.Interfaces.Services;

namespace WebApi.Integration.Tests.Fixtures;

/// <summary>
/// In-memory implementation of IAuditBlobStorage for integration testing.
/// Stores blobs in a static dictionary so they persist across scoped instances.
/// </summary>
internal class TestAuditBlobStorage : IAuditBlobStorage
{
    private static readonly ConcurrentDictionary<string, BlobData> Blobs = new();

    public Task<BlobUploadResult> UploadAsync(
        DateTime partitionBoundary,
        Stream data,
        CancellationToken cancellationToken = default)
    {
        var uri = $"test://audit-archive/{partitionBoundary:yyyy-MM}.json";

        using var memoryStream = new MemoryStream();
        data.CopyTo(memoryStream);
        var bytes = memoryStream.ToArray();

        var hash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

        Blobs[uri] = new BlobData(bytes, hash);

        return Task.FromResult(BlobUploadResult.Succeeded(uri, hash, bytes.Length));
    }

    public Task<Stream> DownloadAsync(
        string uri,
        CancellationToken cancellationToken = default)
    {
        if (!Blobs.TryGetValue(uri, out var blob))
        {
            throw new FileNotFoundException($"Blob not found: {uri}");
        }

        return Task.FromResult<Stream>(new MemoryStream(blob.Data));
    }

    public Task<string> GetBlobHashAsync(
        string uri,
        CancellationToken cancellationToken = default)
    {
        if (!Blobs.TryGetValue(uri, out var blob))
        {
            throw new FileNotFoundException($"Blob not found: {uri}");
        }

        return Task.FromResult(blob.Hash);
    }

    public Task<bool> ExistsAsync(
        string uri,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Blobs.ContainsKey(uri));
    }

    /// <summary>
    /// Gets the raw data for a blob (for test verification).
    /// </summary>
    public static byte[]? GetBlobData(string uri) =>
        Blobs.TryGetValue(uri, out var blob) ? blob.Data : null;

    /// <summary>
    /// Clears all stored blobs. Call between tests if needed.
    /// </summary>
    public static void ClearAll() => Blobs.Clear();

    private record BlobData(byte[] Data, string Hash);
}