using Microsoft.AspNetCore.Identity.UI.Services;

namespace RaCeDay.Data;

/// <summary>Development-only: prints confirmation emails to Visual Studio output; does not send mail.</summary>
public sealed class DevelopmentEmailSender : IEmailSender
{
    private readonly ILogger<DevelopmentEmailSender> _logger;

    public DevelopmentEmailSender(ILogger<DevelopmentEmailSender> logger)
    {
        _logger = logger;
    }

    public Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        _logger.LogWarning("DEVELOPMENT EMAIL for {Email} | {Subject} | {Body}", email, subject, htmlMessage);
        return Task.CompletedTask;
    }
}
