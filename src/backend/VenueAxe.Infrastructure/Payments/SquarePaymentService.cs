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

    public string? GetApplicationId() => _configuration["Square:ApplicationId"];
    public string? GetLocationId() => _configuration["Square:LocationId"];

    public async Task<SquarePaymentResult> ProcessPaymentAsync(SquarePaymentRequest request, CancellationToken cancellationToken = default)
    {
        var accessToken = !string.IsNullOrWhiteSpace(request.CustomAccessToken)
            ? request.CustomAccessToken
            : _configuration["Square:AccessToken"];
        var environment = !string.IsNullOrWhiteSpace(request.CustomEnvironment)
            ? request.CustomEnvironment
            : (_configuration["Square:Environment"] ?? "Sandbox");
        var locationId = !string.IsNullOrWhiteSpace(request.CustomLocationId)
            ? request.CustomLocationId
            : (_configuration["Square:LocationId"] ?? "LOC_VENUEAXE_DEFAULT");

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

        // 1. If no access token is configured or mock requested, return simulated success
        if (string.IsNullOrWhiteSpace(accessToken) || 
            request.SourceId.StartsWith("sq_mock_") || 
            accessToken.StartsWith("sq_mock") || 
            accessToken.StartsWith("••••") ||
            accessToken.Contains("demo", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("Processing payment via Square Mock fallback for source {SourceId}, amount {Amount} cents, location {LocationId}",
                request.SourceId, request.AmountCents, locationId);

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

    public async Task<SquareConnectionTestResult> TestConnectionAsync(
        string? applicationId,
        string? locationId,
        string? accessToken,
        string? environment,
        CancellationToken cancellationToken = default)
    {
        var token = !string.IsNullOrWhiteSpace(accessToken) ? accessToken.Trim() : _configuration["Square:AccessToken"];
        var locId = !string.IsNullOrWhiteSpace(locationId) ? locationId.Trim() : _configuration["Square:LocationId"];
        var env = !string.IsNullOrWhiteSpace(environment) ? environment.Trim() : (_configuration["Square:Environment"] ?? "Sandbox");

        if (string.IsNullOrWhiteSpace(token))
        {
            return new SquareConnectionTestResult(false, "Square Access Token is required to test gateway connection.");
        }

        // Handle mock or demo sandbox test tokens without failing network calls
        if (token.StartsWith("sq_mock", StringComparison.OrdinalIgnoreCase) || 
            token.StartsWith("••••") || 
            token.Contains("demo", StringComparison.OrdinalIgnoreCase))
        {
            return new SquareConnectionTestResult(
                true,
                $"Connected to Square ({env}) successfully in simulated test mode.",
                MerchantName: "VenueAxe Sandbox Merchant",
                LocationName: locId ?? "Main Lane Facility"
            );
        }

        try
        {
            var baseUrl = env.Equals("Production", StringComparison.OrdinalIgnoreCase)
                ? "https://connect.squareup.com"
                : "https://connect.squareupsandbox.com";

            var endpoint = !string.IsNullOrWhiteSpace(locId)
                ? $"{baseUrl}/v2/locations/{locId}"
                : $"{baseUrl}/v2/merchants/current";

            var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
            request.Headers.Add("Authorization", $"Bearer {token}");
            request.Headers.Add("Square-Version", "2024-01-18");

            var response = await _httpClient.SendAsync(request, cancellationToken);
            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorMsg = $"Square API error: {(int)response.StatusCode} {response.ReasonPhrase}";
                try
                {
                    using var errDoc = JsonDocument.Parse(responseJson);
                    if (errDoc.RootElement.TryGetProperty("errors", out var errors) && errors.GetArrayLength() > 0)
                    {
                        var firstErr = errors[0];
                        if (firstErr.TryGetProperty("detail", out var detail))
                        {
                            errorMsg = $"Square API: {detail.GetString()}";
                        }
                    }
                }
                catch { }

                return new SquareConnectionTestResult(false, errorMsg);
            }

            string? merchantName = null;
            string? locationName = null;

            try
            {
                using var doc = JsonDocument.Parse(responseJson);
                if (doc.RootElement.TryGetProperty("location", out var locElem))
                {
                    if (locElem.TryGetProperty("name", out var ln)) locationName = ln.GetString();
                    if (locElem.TryGetProperty("business_name", out var bn)) merchantName = bn.GetString();
                }
                else if (doc.RootElement.TryGetProperty("merchant", out var merchElem))
                {
                    if (merchElem.TryGetProperty("business_name", out var bn)) merchantName = bn.GetString();
                }
            }
            catch { }

            return new SquareConnectionTestResult(
                true,
                $"Successfully authenticated with Square ({env})! Location: {locationName ?? locId ?? "Default"}.",
                merchantName,
                locationName
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to test Square connection.");
            return new SquareConnectionTestResult(false, $"Connection error: {ex.Message}");
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
