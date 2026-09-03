namespace CarePoint.Domain.Common;

public static class UserRoleRules
{
    private static readonly HashSet<string> KnownRoles =
        new(StringComparer.Ordinal) { "Admin", "Doctor", "Patient" };

    public static bool TryResolveSingleRole(
        IEnumerable<string> assignedRoles, out string role)
    {
        var roles = assignedRoles.Distinct(StringComparer.Ordinal).ToList();
        if (roles.Count == 1 && KnownRoles.Contains(roles[0]))
        {
            role = roles[0];
            return true;
        }

        role = string.Empty;
        return false;
    }
}
