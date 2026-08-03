namespace CarePoint.Infrastructure.Data;

public static class DatabaseInitializationCommand
{
    public static bool IsRequested(IEnumerable<string> arguments) =>
        arguments.Contains("--initialize-database", StringComparer.OrdinalIgnoreCase);
}
