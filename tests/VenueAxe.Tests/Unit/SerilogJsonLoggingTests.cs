using System;
using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Context;
using Serilog.Core;
using Serilog.Events;
using Serilog.Extensions.Logging;
using Serilog.Formatting;
using Serilog.Formatting.Compact;
using Xunit;

namespace VenueAxe.Tests.Unit;

public class SerilogJsonLoggingTests
{
    private sealed class StringWriterSink : ILogEventSink
    {
        private readonly TextWriter _writer;
        private readonly ITextFormatter _formatter;

        public StringWriterSink(TextWriter writer, ITextFormatter formatter)
        {
            _writer = writer;
            _formatter = formatter;
        }

        public void Emit(LogEvent logEvent)
        {
            _formatter.Format(logEvent, _writer);
        }
    }

    [Fact]
    public void RenderedCompactJsonFormatter_EmitsValidJsonWithStructuredProperties()
    {
        // Arrange
        using var stringWriter = new StringWriter();
        var serilogLogger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .WriteTo.Sink(new StringWriterSink(stringWriter, new RenderedCompactJsonFormatter()))
            .CreateLogger();

        var sessionId = Guid.NewGuid();
        const int laneNumber = 3;
        const int partySize = 6;

        // Act
        serilogLogger.Information(
            "Session {SessionId} on Lane {LaneNumber} started with {PartySize} throwers",
            sessionId, laneNumber, partySize);

        serilogLogger.Dispose();

        // Assert
        var outputJson = stringWriter.ToString().Trim();
        Assert.NotEmpty(outputJson);

        using var doc = JsonDocument.Parse(outputJson);
        var root = doc.RootElement;

        // CLEF format assertions
        Assert.True(root.TryGetProperty("@t", out var timestampElem));
        Assert.NotEmpty(timestampElem.GetString()!);

        Assert.True(root.TryGetProperty("@m", out var messageElem));
        Assert.Equal($"Session {sessionId} on Lane {laneNumber} started with {partySize} throwers", messageElem.GetString());

        // Structured properties assertions
        Assert.True(root.TryGetProperty("SessionId", out var sessionProp));
        Assert.Equal(sessionId.ToString(), sessionProp.GetString());

        Assert.True(root.TryGetProperty("LaneNumber", out var laneProp));
        Assert.Equal(laneNumber, laneProp.GetInt32());

        Assert.True(root.TryGetProperty("PartySize", out var partyProp));
        Assert.Equal(partySize, partyProp.GetInt32());
    }

    [Fact]
    public void RenderedCompactJsonFormatter_FormatsExceptionsInJson()
    {
        // Arrange
        using var stringWriter = new StringWriter();
        var serilogLogger = new LoggerConfiguration()
            .WriteTo.Sink(new StringWriterSink(stringWriter, new RenderedCompactJsonFormatter()))
            .CreateLogger();

        var bookingRef = "VA-99881";

        // Act
        try
        {
            throw new InvalidOperationException("Payment gateway timeout simulation");
        }
        catch (Exception ex)
        {
            serilogLogger.Error(ex, "Failed to process booking confirmation for {BookingReference}", bookingRef);
        }

        serilogLogger.Dispose();

        // Assert
        var outputJson = stringWriter.ToString().Trim();
        using var doc = JsonDocument.Parse(outputJson);
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("@l", out var levelElem));
        Assert.Equal("Error", levelElem.GetString());

        Assert.True(root.TryGetProperty("@x", out var exceptionElem));
        Assert.Contains("Payment gateway timeout simulation", exceptionElem.GetString());
        Assert.Contains("InvalidOperationException", exceptionElem.GetString());

        Assert.True(root.TryGetProperty("BookingReference", out var refProp));
        Assert.Equal(bookingRef, refProp.GetString());
    }

    [Fact]
    public void MicrosoftExtensionsLoggingBridge_CapturesStructuredPropertiesInJson()
    {
        // Arrange
        using var stringWriter = new StringWriter();
        var serilogLogger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .WriteTo.Sink(new StringWriterSink(stringWriter, new RenderedCompactJsonFormatter()))
            .CreateLogger();

        using var loggerFactory = new SerilogLoggerFactory(serilogLogger);
        var msLogger = loggerFactory.CreateLogger("VenueAxe.Services.BookingService");

        var venueId = Guid.NewGuid();
        const string paymentId = "sq_pay_test123";
        const int amountCents = 7500;

        // Act
        msLogger.LogInformation(
            "Payment {PaymentId} recorded for venue {VenueId} with total {AmountCents} cents",
            paymentId, venueId, amountCents);

        // Assert
        var outputJson = stringWriter.ToString().Trim();
        using var doc = JsonDocument.Parse(outputJson);
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("PaymentId", out var paymentProp));
        Assert.Equal(paymentId, paymentProp.GetString());

        Assert.True(root.TryGetProperty("VenueId", out var venueProp));
        Assert.Equal(venueId.ToString(), venueProp.GetString());

        Assert.True(root.TryGetProperty("AmountCents", out var amountProp));
        Assert.Equal(amountCents, amountProp.GetInt32());

        Assert.True(root.TryGetProperty("SourceContext", out var sourceProp));
        Assert.Equal("VenueAxe.Services.BookingService", sourceProp.GetString());
    }

    [Fact]
    public void LogContext_EnrichesJsonOutputWithAmbientProperties()
    {
        // Arrange
        using var stringWriter = new StringWriter();
        var serilogLogger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .WriteTo.Sink(new StringWriterSink(stringWriter, new RenderedCompactJsonFormatter()))
            .CreateLogger();

        // Act
        using (LogContext.PushProperty("CorrelationId", "req-xyz-789"))
        using (LogContext.PushProperty("TenantId", "tenant-alpha"))
        {
            serilogLogger.Information("Processing public booking endpoint");
        }

        serilogLogger.Dispose();

        // Assert
        var outputJson = stringWriter.ToString().Trim();
        using var doc = JsonDocument.Parse(outputJson);
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("CorrelationId", out var corrProp));
        Assert.Equal("req-xyz-789", corrProp.GetString());

        Assert.True(root.TryGetProperty("TenantId", out var tenantProp));
        Assert.Equal("tenant-alpha", tenantProp.GetString());
    }

    [Fact]
    public void MinimumLevelOverrides_FilterMessagesCorrectly()
    {
        // Arrange
        using var stringWriter = new StringWriter();
        var serilogLogger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .WriteTo.Sink(new StringWriterSink(stringWriter, new RenderedCompactJsonFormatter()))
            .CreateLogger();

        var aspNetLogger = serilogLogger.ForContext(Serilog.Core.Constants.SourceContextPropertyName, "Microsoft.AspNetCore.Hosting.Diagnostics");
        var appLogger = serilogLogger.ForContext(Serilog.Core.Constants.SourceContextPropertyName, "VenueAxe.Web.Areas.Admin.Controllers.AuthController");

        // Act
        aspNetLogger.Information("This ASP.NET Core information log should be filtered out");
        appLogger.Information("This application information log should be emitted");

        serilogLogger.Dispose();

        // Assert
        var output = stringWriter.ToString().Trim();
        Assert.DoesNotContain("filtered out", output);
        Assert.Contains("This application information log should be emitted", output);
    }
}
