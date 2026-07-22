namespace CarePoint.Domain.Exceptions;

/// <summary>
/// Thrown when a request contains invalid data. Maps to HTTP 400.
/// </summary>
public class BadRequestException : Exception
{
    public BadRequestException(string message) : base(message) { }
}
