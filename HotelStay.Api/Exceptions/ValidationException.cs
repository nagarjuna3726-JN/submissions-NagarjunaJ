namespace HotelStay.Api.Exceptions;

/// <summary>
/// Exception thrown when validation fails
/// </summary>
public class ValidationException : ApiException
{
    public ValidationException(
        string message,
        Dictionary<string, string[]>? errors = null,
        Exception? innerException = null)
        : base(message, 400, "VALIDATION_ERROR", errors, innerException)
    {
    }
}
