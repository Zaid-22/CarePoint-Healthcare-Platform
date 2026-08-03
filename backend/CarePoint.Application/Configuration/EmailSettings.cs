namespace CarePoint.Application.Configuration;

/// <summary>
/// SMTP and URL settings used to deliver password reset links.
/// </summary>
public class EmailSettings
{
    public const string SectionName = "EmailSettings";

    public string FromAddress { get; set; } = string.Empty;
    public string FromName { get; set; } = "CarePoint";
    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;
    public string SmtpUser { get; set; } = string.Empty;
    public string SmtpPassword { get; set; } = string.Empty;
    public bool UseSsl { get; set; } = true;
    public string PasswordResetUrl { get; set; } = "http://localhost:5173/reset-password";
}
