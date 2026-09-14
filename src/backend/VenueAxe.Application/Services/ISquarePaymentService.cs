using System.Threading;
using System.Threading.Tasks;
using VenueAxe.DTOs;

namespace VenueAxe.Services;

public record SquareConnectionTestResult(
    bool Success,
    string Message,
    string? MerchantName = null,
    string? LocationName = null
);

public interface ISquarePaymentService
{
    Task<SquarePaymentResult> ProcessPaymentAsync(SquarePaymentRequest request, CancellationToken cancellationToken = default);
    Task<bool> VerifyWebhookSignatureAsync(string requestBody, string signatureHeader, string webhookUrl);
    Task<SquareConnectionTestResult> TestConnectionAsync(string? applicationId, string? locationId, string? accessToken, string? environment, CancellationToken cancellationToken = default)
        => Task.FromResult(new SquareConnectionTestResult(true, "Simulated mock test connection"));
    string? GetApplicationId();
    string? GetLocationId();
}