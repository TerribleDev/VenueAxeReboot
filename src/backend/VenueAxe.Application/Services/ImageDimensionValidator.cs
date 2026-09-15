using System;
using System.IO;

namespace VenueAxe.Application.Services;

public record ImageValidationResult(bool IsValid, int Width, int Height, string? ErrorMessage = null);

public static class ImageDimensionValidator
{
    public const int RecommendedDimension = 512;
    public const int MinDimension = 128;
    public const int MaxDimension = 1024;
    public const long MaxSizeBytes = 2 * 1024 * 1024; // 2 MB

    public static ImageValidationResult Validate(Stream stream, string contentType, string extension)
    {
        if (stream == null || stream.Length == 0)
        {
            return new ImageValidationResult(false, 0, 0, "No file content uploaded.");
        }

        if (stream.Length > MaxSizeBytes)
        {
            return new ImageValidationResult(false, 0, 0, $"File size exceeds the 2MB limit ({stream.Length / 1024} KB).");
        }

        var ext = extension.ToLowerInvariant().TrimStart('.');
        if (ext is not ("png" or "jpg" or "jpeg" or "webp" or "svg"))
        {
            return new ImageValidationResult(false, 0, 0, $"Unsupported image format '.{ext}'. Supported formats are PNG, WebP, SVG, and JPEG.");
        }

        // SVG is vector-based and infinitely scalable
        if (ext == "svg" || contentType.Contains("svg", StringComparison.OrdinalIgnoreCase))
        {
            return new ImageValidationResult(true, RecommendedDimension, RecommendedDimension);
        }

        try
        {
            if (stream.CanSeek)
            {
                stream.Position = 0;
            }

            var (width, height) = TryReadDimensions(stream, ext);
            if (stream.CanSeek)
            {
                stream.Position = 0;
            }

            if (width <= 0 || height <= 0)
            {
                // If header parsing was inconclusive, accept valid image format within size bounds
                return new ImageValidationResult(true, RecommendedDimension, RecommendedDimension);
            }

            if (width < MinDimension || height < MinDimension)
            {
                return new ImageValidationResult(false, width, height,
                    $"Image dimensions ({width}x{height}px) are too small. Minimum required is {MinDimension}x{MinDimension}px (recommended {RecommendedDimension}x{RecommendedDimension}px).");
            }

            if (width > MaxDimension || height > MaxDimension)
            {
                return new ImageValidationResult(false, width, height,
                    $"Image dimensions ({width}x{height}px) exceed the maximum allowed ({MaxDimension}x{MaxDimension}px). Please resize to {RecommendedDimension}x{RecommendedDimension}px.");
            }

            // Aspect ratio check (must be square within 5% tolerance)
            double ratio = (double)width / height;
            if (ratio < 0.95 || ratio > 1.05)
            {
                return new ImageValidationResult(false, width, height,
                    $"Venue icon must be a square image with 1:1 aspect ratio (detected {width}x{height}px). Recommended size is {RecommendedDimension}x{RecommendedDimension}px.");
            }

            return new ImageValidationResult(true, width, height);
        }
        catch
        {
            // Fallback to accepting if stream readable
            return new ImageValidationResult(true, RecommendedDimension, RecommendedDimension);
        }
    }

    private static (int width, int height) TryReadDimensions(Stream stream, string ext)
    {
        using var reader = new BinaryReader(stream, System.Text.Encoding.UTF8, leaveOpen: true);
        var buffer = new byte[Math.Min(4096, (int)stream.Length)];
        int read = reader.Read(buffer, 0, buffer.Length);

        if (ext == "png" && read >= 24)
        {
            // PNG signature check
            if (buffer[0] == 0x89 && buffer[1] == 0x50 && buffer[2] == 0x4E && buffer[3] == 0x47)
            {
                int width = (buffer[16] << 24) | (buffer[17] << 16) | (buffer[18] << 8) | buffer[19];
                int height = (buffer[20] << 24) | (buffer[21] << 16) | (buffer[22] << 8) | buffer[23];
                return (width, height);
            }
        }

        if ((ext == "jpg" || ext == "jpeg") && read >= 4)
        {
            if (buffer[0] == 0xFF && buffer[1] == 0xD8)
            {
                int i = 2;
                while (i < read - 8)
                {
                    if (buffer[i] != 0xFF) { i++; continue; }
                    byte marker = buffer[i + 1];
                    if (marker is 0xC0 or 0xC2) // SOF0 or SOF2
                    {
                        int height = (buffer[i + 5] << 8) | buffer[i + 6];
                        int width = (buffer[i + 7] << 8) | buffer[i + 8];
                        return (width, height);
                    }
                    int length = (buffer[i + 2] << 8) | buffer[i + 3];
                    if (length <= 0) break;
                    i += 2 + length;
                }
            }
        }

        if (ext == "webp" && read >= 30)
        {
            if (buffer[0] == 'R' && buffer[1] == 'I' && buffer[2] == 'F' && buffer[3] == 'F' &&
                buffer[8] == 'W' && buffer[9] == 'E' && buffer[10] == 'B' && buffer[11] == 'P')
            {
                // VP8X
                if (buffer[12] == 'V' && buffer[13] == 'P' && buffer[14] == '8' && buffer[15] == 'X')
                {
                    int width = 1 + (buffer[24] | (buffer[25] << 8) | (buffer[26] << 16));
                    int height = 1 + (buffer[27] | (buffer[28] << 8) | (buffer[29] << 16));
                    return (width, height);
                }
                // Simple VP8 lossy
                if (buffer[12] == 'V' && buffer[13] == 'P' && buffer[14] == '8' && buffer[15] == ' ' && read >= 30)
                {
                    int width = ((buffer[26] | (buffer[27] << 8)) & 0x3fff);
                    int height = ((buffer[28] | (buffer[29] << 8)) & 0x3fff);
                    return (width, height);
                }
            }
        }

        return (0, 0);
    }
}
