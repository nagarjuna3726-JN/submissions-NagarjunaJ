namespace HotelStay.Api.Services;

/// <summary>
/// Validates travel documents for booking
/// </summary>
public interface IDocumentValidator
{
    /// <summary>
    /// Validates document for given destination
    /// </summary>
    /// <param name="destination">Destination city</param>
    /// <param name="documentType">Document type (Passport or NationalID)</param>
    /// <param name="documentNumber">Document number</param>
    /// <returns>Tuple of (isValid, errorMessage)</returns>
    Task<(bool IsValid, string? ErrorMessage)> ValidateAsync(
        string destination,
        string documentType,
        string documentNumber);
}

/// <summary>
/// Implementation of document validator
/// </summary>
public class DocumentValidator : IDocumentValidator
{
    private static readonly HashSet<string> DomesticDestinations = new(StringComparer.OrdinalIgnoreCase)
    {
        "New York",
        "Los Angeles",
        "Chicago"
    };

    private static readonly HashSet<string> InternationalDestinations = new(StringComparer.OrdinalIgnoreCase)
    {
        "London",
        "Paris",
        "Tokyo",
        "Sydney",
        "Dubai"
    };

    private readonly ILogger<DocumentValidator> _logger;

    public DocumentValidator(ILogger<DocumentValidator> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Validates document for given destination
    /// Domestic destinations accept Passport or National ID
    /// International destinations require Passport only
    /// </summary>
    public Task<(bool IsValid, string? ErrorMessage)> ValidateAsync(
        string destination,
        string documentType,
        string documentNumber)
    {
        // Validate inputs
        if (string.IsNullOrWhiteSpace(destination))
            return Task.FromResult((false, "Destination is required"));

        if (string.IsNullOrWhiteSpace(documentType))
            return Task.FromResult((false, "Document type is required"));

        if (string.IsNullOrWhiteSpace(documentNumber))
            return Task.FromResult((false, "Document number is required"));

        // Check destination type
        if (DomesticDestinations.Contains(destination))
        {
            // Domestic: accept Passport or National ID
            if (documentType.Equals("Passport", StringComparison.OrdinalIgnoreCase) ||
                documentType.Equals("NationalID", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation(
                    "Document validation successful: destination={Destination}, documentType={DocumentType}",
                    destination, documentType);

                return Task.FromResult((true, null as string));
            }

            var errorMsg = $"Domestic destination {destination} requires Passport or National ID";
            _logger.LogWarning(
                "Document validation failed: {ErrorMessage}, provided={DocumentType}",
                errorMsg, documentType);

            return Task.FromResult((false, errorMsg));
        }
        else if (InternationalDestinations.Contains(destination))
        {
            // International: Passport required only
            if (documentType.Equals("Passport", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation(
                    "Document validation successful: destination={Destination}, documentType={DocumentType}",
                    destination, documentType);

                return Task.FromResult((true, null as string));
            }

            var errorMsg = $"International destination {destination} requires Passport";
            _logger.LogWarning(
                "Document validation failed: {ErrorMessage}, provided={DocumentType}",
                errorMsg, documentType);

            return Task.FromResult((false, errorMsg));
        }
        else
        {
            var errorMsg = $"Unknown destination: {destination}";
            _logger.LogWarning("Document validation failed: {ErrorMessage}", errorMsg);

            return Task.FromResult((false, errorMsg));
        }
    }
}
