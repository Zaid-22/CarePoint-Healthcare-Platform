namespace CarePoint.Application.Configuration;

public sealed class ClinicTimeSettings
{
    public const string SectionName = "ClinicTime";
    public string TimeZoneId { get; set; } = "Asia/Amman";
}
