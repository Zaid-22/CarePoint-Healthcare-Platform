namespace CarePoint.Domain.Exceptions;

/// <summary>
/// Thrown when an operation conflicts with the current state (e.g., duplicate booking). Maps to HTTP 409.
/// </summary>
public class ConflictException : Exception
{
    public ConflictException(string message) : base(message) { }
}
