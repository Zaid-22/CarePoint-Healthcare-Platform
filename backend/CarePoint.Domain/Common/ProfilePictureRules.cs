namespace CarePoint.Domain.Common;

public static class ProfilePictureRules
{
    public static bool IsPermittedExternalUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return true;
        return Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
               uri.Scheme == Uri.UriSchemeHttps &&
               !string.IsNullOrWhiteSpace(uri.Host);
    }

    public static bool IsPermittedReference(string? value) =>
        IsPermittedExternalUrl(value) ||
        (!string.IsNullOrWhiteSpace(value) &&
         value.StartsWith("/api/doctors/", StringComparison.Ordinal) &&
         !value.Contains("..", StringComparison.Ordinal));
}
