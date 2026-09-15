namespace VenueAxe.Infrastructure.Storage;

public class StorageOptions
{
    public const string SectionName = "Storage";

    /// <summary>
    /// Storage provider: "LocalStorage" (development) or "Backblaze" (production S3-compatible).
    /// </summary>
    public string Provider { get; set; } = "LocalStorage";

    /// <summary>
    /// Base relative or absolute path for local disk uploads in development.
    /// </summary>
    public string LocalUploadPath { get; set; } = "uploads";

    /// <summary>
    /// Backblaze B2 S3-compatible configuration.
    /// </summary>
    public BackblazeOptions Backblaze { get; set; } = new();
}

public class BackblazeOptions
{
    public string ServiceUrl { get; set; } = string.Empty;
    public string BucketName { get; set; } = string.Empty;
    public string KeyId { get; set; } = string.Empty;
    public string ApplicationKey { get; set; } = string.Empty;
    public string PublicBaseUrl { get; set; } = string.Empty;
}
