namespace CarePoint.Domain.Exceptions;

/// <summary>
/// Thrown when a user lacks permission to access a resource. Maps to HTTP 403.
/// </summary>
public class ForbiddenException : Exception
{
    public ForbiddenException(string message) : base(message) { }

    public ForbiddenException()
        : base("You do not have permission to access this resource.") { }
}
