using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using VenueAxe.DTOs;

namespace VenueAxe.Services;

public interface ISquarePaymentService
{
    Task<SquarePaymentResult> ProcessPaymentAsync(SquarePaymentRequest request, CancellationToken cancellationToken = default);
    Task<bool> VerifyWebhookSignatureAsync(string requestBody, string signatureHeader, string webhookUrl);
}

public class SquarePaymentService : ISquarePaymentService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SquarePaymentService> _logger;
    private readonly HttpClient _httpClient;

    public SquarePaymentService(IConfiguration configuration, ILogger<SquarePaymentService> logger, HttpClient? httpClient = null)
    {
        _configuration = configuration;
        _logger = logger;
        _httpClient = httpClient ?? new HttpClient();
    }

    public async Task<SquarePaymentResult> ProcessPaymentAsync(SquarePaymentRequest request, CancellationToken cancellationToken = default)
    {
        var accessToken = _configuration["Square:AccessToken"];
        var environment = _configuration["Square:Environment"] ?? "Sandbox";
        var locationId = _configuration["Square:LocationId"] ?? "LOC_VENUEAXE_DEFAULT";

        // 1. Handle Developer / Sandbox Mock Nonces
        if (string.IsNullOrWhiteSpace(accessToken) || request.SourceId.StartsWith("cnon:") || request.SourceId.StartsWith("sq_test_"))
        {
            _logger.LogInformation("Processing payment via Square Sandbox/Mock provider for source {SourceId}, amount {Amount} cents",
                request.SourceId, request.AmountCents);

            if (request.SourceId.Equals("cnon:card-nonce-declined", StringComparison.OrdinalIgnoreCase))
            {
                return new SquarePaymentResult(
                    false,
                    null,
                    null,
                    null,
                    "FAILED",
                    "Card was declined by issuing bank (Square Mock Decline)."
                );
            }

            var mockPaymentId = $"sq_pay_{Guid.NewGuid().ToString("N")[..16]}";
            var mockOrderId = $"sq_ord_{Guid.NewGuid().ToString("N")[..16]}";
            var receiptUrl = $"https://squareupsandbox.com/receipt/preview/{mockPaymentId}";

            return new SquarePaymentResult(
                true,
                mockPaymentId,
                mockOrderId,
                receiptUrl,
                "COMPLETED"
            );
        }

        // 2. Live / Sandbox HTTP Request to Square Payments API
        try
        {
            var baseUrl = environment.Equals("Production", StringComparison.OrdinalIgnoreCase)
                ? "https://connect.squareup.com"
                : "https://connect.squareupsandbox.com";

            var idempotencyKey = Guid.NewGuid().ToString();
            var payload = new
            {
                source_id = request.SourceId,
                idempotency_key = idempotencyKey,
                amount_money = new
                {
                    amount = request.AmountCents,
                    currency = string.IsNullOrWhiteSpace(request.Currency) ? "USD" : request.Currency.ToUpperInvariant()
                },
                location_id = locationId,
                reference_id = request.ReferenceId,
                buyer_email_address = request.CustomerEmail,
                verification_token = request.VerificationToken
            };

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/v2/payments")
            {
                Content = JsonContent.Create(payload)
            };
            httpRequest.Headers.Add("Authorization", $"Bearer {accessToken}");
            httpRequest.Headers.Add("Square-Version", "2024-01-18");

            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Square payment API returned non-success code {StatusCode}: {Response}",
                    response.StatusCode, responseJson);

                return new SquarePaymentResult(
                    false,
                    null,
                    null,
                    null,
                    "FAILED",
                    $"Square payment rejected: {response.StatusCode}"
                );
            }

            using var doc = JsonDocument.Parse(responseJson);
            if (doc.RootElement.TryGetProperty("payment", out var paymentElem))
            {
                var paymentId = paymentElem.GetProperty("id").GetString();
                var status = paymentElem.GetProperty("status").GetString();
                var orderId = paymentElem.TryGetProperty("order_id", out var o) ? o.GetString() : null;
                var receiptUrl = paymentElem.TryGetProperty("receipt_url", out var r) ? r.GetString() : null;

                bool isSuccess = status is "COMPLETED" or "APPROVED";
                return new SquarePaymentResult(isSuccess, paymentId, orderId, receiptUrl, status);
            }

            return new SquarePaymentResult(false, null, null, null, "FAILED", "Unexpected Square API response structure.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception encountered during Square payment execution.");
            return new SquarePaymentResult(false, null, null, null, "ERROR", ex.Message);
        }
    }

    public Task<bool> VerifyWebhookSignatureAsync(string requestBody, string signatureHeader, string webhookUrl)
    {
        var signatureKey = _configuration["Square:WebhookSignatureKey"];
        if (string.IsNullOrWhiteSpace(signatureKey))
        {
            // If not configured in development, allow sandbox payloads
            return Task.FromResult(true);
        }

        try
        {
            var payload = webhookUrl + requestBody;
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(signatureKey));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            var expectedSignature = Convert.ToBase64String(hash);

            return Task.FromResult(CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(expectedSignature),
                Encoding.UTF8.GetBytes(signatureHeader)
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying Square webhook signature.");
            return Task.FromResult(false);
        }
    }
}
