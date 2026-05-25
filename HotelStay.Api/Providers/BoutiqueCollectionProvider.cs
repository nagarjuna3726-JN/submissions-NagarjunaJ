namespace HotelStay.Api.Providers;

/// <summary>
/// BoutiqueCollection provider - Premium boutique hotels (Live Tweak Scenario).
/// 
/// Characteristics:
/// - Rate = base nightly rate + boutique_fee (£15/night)
/// - Supports Deluxe & Suite only (no Standard rooms)
/// - CancellationPolicy: FreeCancellation up to 72h before check-in
/// - Returns availability as boolean per room type
/// - Returns deterministic stub responses
/// 
/// Constraint: Added without modifying:
/// - IHotelProvider interface
/// - Aggregation or booking orchestration logic
/// - Existing provider implementations
/// - Only adds new implementation + DI registration
/// </summary>
public class BoutiqueCollectionProvider : IHotelProvider
{
    private readonly ILogger<BoutiqueCollectionProvider> _logger;
    private const decimal BoutiqueFee = 15m; // £15/night premium fee

    public BoutiqueCollectionProvider(ILogger<BoutiqueCollectionProvider> logger)
    {
        _logger = logger;
    }

    public string ProviderId => "boutique-collection";

    /// <summary>
    /// Searches for boutique hotels using deterministic stub data.
    /// Only Deluxe and Suite room types are supported.
    /// </summary>
    public Task<IEnumerable<ProviderHotelResult>> SearchAsync(
        string destination,
        DateOnly checkIn,
        DateOnly checkOut,
        string? roomType = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "BoutiqueCollection search: destination={Destination}, checkIn={CheckIn}, checkOut={CheckOut}, roomType={RoomType}",
            destination, checkIn, checkOut, roomType ?? "all");

        var numberOfNights = (checkOut.DayNumber - checkIn.DayNumber);

        // Boutique hotels support only Deluxe and Suite (no Standard rooms)
        var allResults = new List<ProviderHotelResult>
        {
            new()
            {
                HotelId = "elegance-suites-deluxe",
                Name = "Elegance Suites",
                RoomType = "Deluxe",
                PricePerNight = 180m + BoutiqueFee, // Base 180 + 15 boutique fee = 195
                NumberOfNights = numberOfNights,
                Rating = 4,
                Amenities = "WiFi, Concierge, Boutique Amenities",
                CancellationPolicy = "FreeCancellation",
                CancellationPolicyDetails = "Free cancellation up to 72 hours before check-in",
                IsAvailable = true
            },
            new()
            {
                HotelId = "elegance-suites-suite",
                Name = "Elegance Suites",
                RoomType = "Suite",
                PricePerNight = 300m + BoutiqueFee, // Base 300 + 15 boutique fee = 315
                NumberOfNights = numberOfNights,
                Rating = 4,
                Amenities = "WiFi, Concierge, Boutique Amenities, Private Terrace",
                CancellationPolicy = "FreeCancellation",
                CancellationPolicyDetails = "Free cancellation up to 72 hours before check-in",
                IsAvailable = true
            },
            new()
            {
                HotelId = "luxury-retreat-deluxe",
                Name = "Luxury Retreat",
                RoomType = "Deluxe",
                PricePerNight = 220m + BoutiqueFee, // Base 220 + 15 boutique fee = 235
                NumberOfNights = numberOfNights,
                Rating = 5,
                Amenities = "WiFi, Concierge, Luxury Boutique, Spa Access",
                CancellationPolicy = "FreeCancellation",
                CancellationPolicyDetails = "Free cancellation up to 72 hours before check-in",
                IsAvailable = true
            },
            new()
            {
                HotelId = "luxury-retreat-suite",
                Name = "Luxury Retreat",
                RoomType = "Suite",
                PricePerNight = 380m + BoutiqueFee, // Base 380 + 15 boutique fee = 395
                NumberOfNights = numberOfNights,
                Rating = 5,
                Amenities = "WiFi, Concierge, Luxury Boutique, Spa, Rooftop Pool",
                CancellationPolicy = "FreeCancellation",
                CancellationPolicyDetails = "Free cancellation up to 72 hours before check-in",
                IsAvailable = true
            }
        };

        // Filter by roomType if specified
        // Note: BoutiqueCollection only supports Deluxe and Suite
        var filtered = string.IsNullOrWhiteSpace(roomType)
            ? allResults
            : allResults.Where(r => r.RoomType.Equals(roomType, StringComparison.OrdinalIgnoreCase)).ToList();

        _logger.LogInformation(
            "BoutiqueCollection returning {Count} results",
            filtered.Count);

        return Task.FromResult<IEnumerable<ProviderHotelResult>>(filtered);
    }
}
