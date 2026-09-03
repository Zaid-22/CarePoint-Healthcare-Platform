namespace CarePoint.Tests;

[AttributeUsage(AttributeTargets.Method)]
public sealed class SqlServerFactAttribute : FactAttribute
{
    public SqlServerFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("CAREPOINT_TEST_SQL")))
        {
            Skip = "Set CAREPOINT_TEST_SQL to run the SQL Server integration test.";
        }
    }
}
