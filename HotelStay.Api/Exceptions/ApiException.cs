namespace HotelStay.Api.Exceptions;

/// <summary>
/// Base custom exception for API-specific errors
/// </summary>
public class ApiException : Exception
{
    public int StatusCode { get; set; }
    public string? ErrorCode { get; set; }
    public Dictionary<string, string[]>? Errors { get; set; }

    public ApiException(
        string message,
        int statusCode = 500,
        string? errorCode = null,
        Dictionary<string, string[]>? errors = null,
        Exception? innerException = null)
        : base(message, innerException)
    {
        StatusCode = statusCode;
        ErrorCode = errorCode;
        Errors = errors;
    }
}
