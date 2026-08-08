namespace CarePoint.Application.Configuration;

public sealed class MedicalDocumentSettings
{
    public const string SectionName = "MedicalDocuments";

    public long MaxBytesPerPatient { get; set; } = 100 * 1024 * 1024;
    public int UploadPermitLimit { get; set; } = 20;
    public int UploadWindowMinutes { get; set; } = 60;
}
