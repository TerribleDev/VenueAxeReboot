using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VenueAxe.Services;

namespace VenueAxe.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Route("api/admin/email")]
[ApiController]
[Authorize]
public class EmailController : ControllerBase
{
    private readonly IEmailService _emailService;
    private readonly SmtpOptions _smtpOptions;
    private readonly ILogger<EmailController> _logger;

    public EmailController(
        IEmailService emailService,
        IOptions<SmtpOptions> smtpOptions,
        ILogger<EmailController> logger)
    {
        _emailService = emailService;
        _smtpOptions = smtpOptions.Value;
        _logger = logger;
    }

    [HttpGet("settings")]
    public ActionResult<object> GetEmailSettings()
    {
        return Ok(new
        {
            server = _smtpOptions.Host,
            port = _smtpOptions.Port,
            sender = _smtpOptions.FromEmail,
            senderName = _smtpOptions.FromName,
            ssl = _smtpOptions.EnableSsl
        });
    }

    [HttpPost("test")]
    public async Task<ActionResult<SendTestEmailResponse>> SendTestEmail([FromBody] SendTestEmailRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ToEmail))
        {
            return BadRequest(new SendTestEmailResponse(false, "Recipient email address is required.", _smtpOptions.Host, _smtpOptions.Port, _smtpOptions.FromEmail));
        }

        try
        {
            _logger.LogInformation("Admin requested test email to {Recipient} for venue {VenueName}", request.ToEmail, request.VenueName ?? "Default");
            var success = await _emailService.SendTestEmailAsync(request.ToEmail, request.VenueName);

            if (success)
            {
                return Ok(new SendTestEmailResponse(
                    true,
                    $"Test email successfully delivered to {request.ToEmail} via {_smtpOptions.Host}:{_smtpOptions.Port}.",
                    _smtpOptions.Host,
                    _smtpOptions.Port,
                    _smtpOptions.FromEmail
                ));
            }

            return StatusCode(500, new SendTestEmailResponse(
                false,
                $"Failed to deliver test email to {request.ToEmail}. Check server logs for details.",
                _smtpOptions.Host,
                _smtpOptions.Port,
                _smtpOptions.FromEmail
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception sending test email to {Recipient}", request.ToEmail);
            return StatusCode(500, new SendTestEmailResponse(
                false,
                $"SMTP Error: {ex.Message}",
                _smtpOptions.Host,
                _smtpOptions.Port,
                _smtpOptions.FromEmail
            ));
        }
    }
}
