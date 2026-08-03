using System.Net;
using System.Net.Mail;
using CarePoint.Application.Configuration;
using CarePoint.Application.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CarePoint.Infrastructure.Services;

public class SmtpPasswordResetEmailSender : IPasswordResetEmailSender
{
    private readonly EmailSettings _settings;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<SmtpPasswordResetEmailSender> _logger;

    public SmtpPasswordResetEmailSender(
        IOptions<EmailSettings> settings,
        IHostEnvironment environment,
        ILogger<SmtpPasswordResetEmailSender> logger)
    {
        _settings = settings.Value;
        _environment = environment;
        _logger = logger;
    }

    public async Task SendAsync(string recipientEmail, string resetUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.SmtpHost) || string.IsNullOrWhiteSpace(_settings.FromAddress))
        {
            if (_environment.IsDevelopment())
            {
                _logger.LogInformation("Password reset link for {RecipientEmail}: {ResetUrl}", recipientEmail, resetUrl);
                return;
            }

            throw new InvalidOperationException("EmailSettings must be configured to send password reset emails.");
        }

        using var message = new MailMessage
        {
            From = new MailAddress(_settings.FromAddress, _settings.FromName),
            Subject = "Reset your CarePoint password",
            Body = $"<p>We received a request to reset your CarePoint password.</p><p><a href=\"{WebUtility.HtmlEncode(resetUrl)}\">Reset your password</a></p><p>If you did not make this request, you can safely ignore this email.</p>",
            IsBodyHtml = true
        };
        message.To.Add(recipientEmail);

        using var client = new SmtpClient(_settings.SmtpHost, _settings.SmtpPort)
        {
            EnableSsl = _settings.UseSsl,
            UseDefaultCredentials = false,
            Credentials = string.IsNullOrWhiteSpace(_settings.SmtpUser)
                ? CredentialCache.DefaultNetworkCredentials
                : new NetworkCredential(_settings.SmtpUser, _settings.SmtpPassword)
        };

        await client.SendMailAsync(message, cancellationToken);
    }
}
