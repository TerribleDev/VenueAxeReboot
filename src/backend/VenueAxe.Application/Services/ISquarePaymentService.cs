using System.Threading;
using System.Threading.Tasks;
using VenueAxe.DTOs;

namespace VenueAxe.Services;

public interface ISquarePaymentService
{
    Task<SquarePaymentResult> ProcessPaymentAsync(SquarePaymentRequest request, CancellationToken cancellationToken = default);
    Task<bool> VerifyWebhookSignatureAsync(string requestBody, string signatureHeader, string webhookUrl);
    string? GetApplicationId();
    string? GetLocationId();
}