using Application.Interfaces.Services;

namespace Infrastructure.Services;

/// <summary>
/// A placeholder implementation of <see cref="IAuditBlobStorage"/> that has not been implemented yet.
/// </summary>
/// <remarks>
/// All methods in this service throw a <see cref="NotImplementedException"/>. This class is intended to be used
/// as a placeholder during development. Replace with an Azure Blob Storage, AWS S3, or other immutable
/// storage implementation for production use.
///
/// For Azure Blob Storage with immutable policies:
/// - Use Azure.Storage.Blobs package
/// - Configure immutable blob storage with legal hold or time-based retention
/// - Enable versioning for audit trail
///
/// For AWS S3 with Object Lock:
/// - Use AWSSDK.S3 package
/// - Configure Object Lock with Governance or Compliance mode
/// - Enable versioning
/// </remarks>
public class NotImplementedAuditBlobStorage : IAuditBlobStorage
{
    public Task<BlobUploadResult> UploadAsync(
        DateTime partitionBoundary,
        Stream data,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException(
            "IAuditBlobStorage is not implemented. " +
            "Replace NotImplementedAuditBlobStorage with an Azure Blob Storage or AWS S3 implementation.");
    }

    public Task<Stream> DownloadAsync(
        string uri,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException(
            "IAuditBlobStorage is not implemented. " +
            "Replace NotImplementedAuditBlobStorage with an Azure Blob Storage or AWS S3 implementation.");
    }

    public Task<string> GetBlobHashAsync(
        string uri,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException(
            "IAuditBlobStorage is not implemented. " +
            "Replace NotImplementedAuditBlobStorage with an Azure Blob Storage or AWS S3 implementation.");
    }

    public Task<bool> ExistsAsync(
        string uri,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException(
            "IAuditBlobStorage is not implemented. " +
            "Replace NotImplementedAuditBlobStorage with an Azure Blob Storage or AWS S3 implementation.");
    }
}