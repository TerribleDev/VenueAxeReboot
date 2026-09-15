using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VenueAxe.Application.Services;

namespace VenueAxe.Infrastructure.Storage;

public class LocalVenueAssetStorageService : IVenueAssetStorageService
{
    private readonly StorageOptions _options;
    private readonly ILogger<LocalVenueAssetStorageService> _logger;
    private readonly string _targetDirectory;

    public LocalVenueAssetStorageService(
        IOptions<StorageOptions> options,
        ILogger<LocalVenueAssetStorageService> logger)
    {
        _options = options.Value;
        _logger = logger;

        // Resolve uploads path relative to AppContext.BaseDirectory or wwwroot
        var baseDir = AppContext.BaseDirectory;
        // Search upwards for wwwroot or src/backend/VenueAxe.Web/wwwroot
        var candidate = Path.Combine(baseDir, "wwwroot", "uploads", "venue-icons");
        if (!Directory.Exists(Path.Combine(baseDir, "wwwroot")))
        {
            // In dev, running from bin/Debug/net10.0
            var solutionDir = FindDirectoryUpwards(baseDir, "src");
            if (solutionDir != null)
            {
                candidate = Path.Combine(solutionDir, "backend", "VenueAxe.Web", "wwwroot", "uploads", "venue-icons");
            }
        }

        _targetDirectory = candidate;
        Directory.CreateDirectory(_targetDirectory);
    }

    public async Task<string> UploadVenueIconAsync(
        Guid venueId,
        Stream fileStream,
        string contentType,
        string extension,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = ".png";
        }
        if (!extension.StartsWith('.'))
        {
            extension = "." + extension;
        }

        var fileName = $"venue-{venueId:N}-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}{extension.ToLowerInvariant()}";
        var fullPath = Path.Combine(_targetDirectory, fileName);

        using (var outputStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await fileStream.CopyToAsync(outputStream, cancellationToken);
        }

        _logger.LogInformation("Saved venue icon locally to {FilePath}", fullPath);
        return $"/uploads/venue-icons/{fileName}";
    }

    public Task DeleteVenueIconAsync(string fileUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fileUrl)) return Task.CompletedTask;

        try
        {
            var fileName = Path.GetFileName(fileUrl);
            if (!string.IsNullOrEmpty(fileName))
            {
                var fullPath = Path.Combine(_targetDirectory, fileName);
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                    _logger.LogInformation("Deleted local venue icon from {FilePath}", fullPath);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete local venue icon file from {FileUrl}", fileUrl);
        }

        return Task.CompletedTask;
    }

    private static string? FindDirectoryUpwards(string startDir, string targetDirName)
    {
        var current = new DirectoryInfo(startDir);
        while (current != null)
        {
            var target = Path.Combine(current.FullName, targetDirName);
            if (Directory.Exists(target))
            {
                return target;
            }
            current = current.Parent;
        }
        return null;
    }
}
