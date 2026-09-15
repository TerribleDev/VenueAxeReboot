using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VenueAxe.Application.Services;

namespace VenueAxe.Infrastructure.Storage;

public class BackblazeVenueAssetStorageService : IVenueAssetStorageService
{
    private readonly StorageOptions _options;
    private readonly ILogger<BackblazeVenueAssetStorageService> _logger;
    private readonly IAmazonS3 _s3Client;

    public BackblazeVenueAssetStorageService(
        IOptions<StorageOptions> options,
        ILogger<BackblazeVenueAssetStorageService> logger)
    {
        _options = options.Value;
        _logger = logger;

        var b2 = _options.Backblaze;
        var s3Config = new AmazonS3Config
        {
            ServiceURL = string.IsNullOrWhiteSpace(b2.ServiceUrl) ? "https://s3.us-west-004.backblazeb2.com" : b2.ServiceUrl,
            ForcePathStyle = true
        };

        _s3Client = new AmazonS3Client(b2.KeyId, b2.ApplicationKey, s3Config);
    }

    public async Task<string> UploadVenueIconAsync(
        Guid venueId,
        Stream fileStream,
        string contentType,
        string extension,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(extension)) extension = ".png";
        if (!extension.StartsWith('.')) extension = "." + extension;

        var b2 = _options.Backblaze;
        var key = $"venue-icons/venue-{venueId:N}-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}{extension.ToLowerInvariant()}";

        var putRequest = new PutObjectRequest
        {
            BucketName = b2.BucketName,
            Key = key,
            InputStream = fileStream,
            ContentType = string.IsNullOrWhiteSpace(contentType) ? "image/png" : contentType,
            CannedACL = S3CannedACL.PublicRead
        };

        await _s3Client.PutObjectAsync(putRequest, cancellationToken);
        _logger.LogInformation("Uploaded venue icon to Backblaze B2 bucket {Bucket}: {ObjectKey}", b2.BucketName, key);

        if (!string.IsNullOrWhiteSpace(b2.PublicBaseUrl))
        {
            return $"{b2.PublicBaseUrl.TrimEnd('/')}/{key}";
        }

        return $"{b2.ServiceUrl.TrimEnd('/')}/{b2.BucketName}/{key}";
    }

    public async Task DeleteVenueIconAsync(string fileUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fileUrl)) return;

        try
        {
            var b2 = _options.Backblaze;
            var uri = new Uri(fileUrl);
            var key = uri.AbsolutePath.TrimStart('/');
            if (key.StartsWith(b2.BucketName + "/"))
            {
                key = key.Substring(b2.BucketName.Length + 1);
            }

            var deleteRequest = new DeleteObjectRequest
            {
                BucketName = b2.BucketName,
                Key = key
            };

            await _s3Client.DeleteObjectAsync(deleteRequest, cancellationToken);
            _logger.LogInformation("Deleted venue icon from Backblaze B2 bucket {Bucket}: {ObjectKey}", b2.BucketName, key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete Backblaze B2 venue icon from {FileUrl}", fileUrl);
        }
    }
}
