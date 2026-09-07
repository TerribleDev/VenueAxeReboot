using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using VenueAxe.DTOs;
using VenueAxe.Services;
using Xunit;

namespace VenueAxe.Tests.Unit;

public class SquarePaymentServiceTests
{
    private readonly ISquarePaymentService _squareService;

    public SquarePaymentServiceTests()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Square:Environment"] = "Sandbox",
                ["Square:LocationId"] = "LOC_TEST_123"
            })
            .Build();

        _squareService = new SquarePaymentService(config, NullLogger<SquarePaymentService>.Instance);
    }

    [Fact]
    public async Task ProcessPayment_ValidSandboxNonce_ReturnsSuccessfulPaymentId()
    {
        var request = new SquarePaymentRequest(
            SourceId: "cnon:card-nonce-ok",
            AmountCents: 7000,
            Currency: "USD",
            CustomerEmail: "guest@example.com",
            ReferenceId: "VA-12345"
        );

        var result = await _squareService.ProcessPaymentAsync(request);

        Assert.True(result.Success);
        Assert.NotNull(result.PaymentId);
        Assert.StartsWith("sq_pay_", result.PaymentId);
        Assert.Equal("COMPLETED", result.Status);
    }

    [Fact]
    public async Task ProcessPayment_DeclinedSandboxNonce_ReturnsFailureWithErrorMessage()
    {
        var request = new SquarePaymentRequest(
            SourceId: "cnon:card-nonce-declined",
            AmountCents: 5000,
            Currency: "USD"
        );

        var result = await _squareService.ProcessPaymentAsync(request);

        Assert.False(result.Success);
        Assert.Null(result.PaymentId);
        Assert.Contains("declined", result.ErrorMessage?.ToLowerInvariant() ?? "");
    }

    [Fact]
    public async Task VerifyWebhookSignature_EmptyKeyInDev_AllowsSignature()
    {
        var isValid = await _squareService.VerifyWebhookSignatureAsync("{}", "mock-sig", "http://localhost:5280/api/public/webhooks/square");
        Assert.True(isValid);
    }
}
