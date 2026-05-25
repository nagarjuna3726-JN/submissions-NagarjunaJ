using HotelStay.Api.Models;

namespace HotelStay.Api.Services;

/// <summary>
/// Aggregates hotel search results from multiple providers
/// </summary>
public interface IHotelAggregator
{
    /// <summary>
    /// Searches all registered providers for available hotels
    /// </summary>
    Task<IEnumerable<HotelSearchResult>> SearchHotelsAsync(
        string destination,
        DateOnly checkIn,
        DateOnly checkOut,
        string? roomType = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementation of hotel aggregator
/// </summary>
public class HotelAggregator : IHotelAggregator
{
    private readonly IEnumerable<IHotelProvider> _providers;
    private readonly ILogger<HotelAggregator> _logger;

    public HotelAggregator(IEnumerable<IHotelProvider> providers, ILogger<HotelAggregator> logger)
    {
        _providers = providers;
        _logger = logger;
    }

    /// <summary>
    /// Searches all providers in parallel and returns normalized results
    /// </summary>
    public async Task<IEnumerable<HotelSearchResult>> SearchHotelsAsync(
        string destination,
        DateOnly checkIn,
        DateOnly checkOut,
        string? roomType = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(destination))
            throw new ArgumentException("Destination cannot be empty", nameof(destination));

        if (checkOut <= checkIn)
            throw new ArgumentException("Check-out date must be after check-in date");

        _logger.LogInformation(
            "Hotel search started: destination={Destination}, checkIn={CheckIn}, checkOut={CheckOut}, roomType={RoomType}",
            destination, checkIn, checkOut, roomType ?? "all");

        // Query all providers in parallel
        var tasks = _providers.Select(provider =>
            SearchSingleProviderAsync(provider, destination, checkIn, checkOut, roomType, cancellationToken));

        var results = await Task.WhenAll(tasks);

        // Flatten results and filter unavailable hotels
        var allResults = results
            .SelectMany(r => r)
            .Where(r => r.Available)
            .OrderBy(r => r.TotalPrice)
            .ThenBy(r => r.HotelName)
            .ToList();

        _logger.LogInformation(
            "Hotel search completed: destination={Destination}, resultCount={ResultCount}",
            destination, allResults.Count);

        return allResults;
    }

    /// <summary>
    /// Searches a single provider and normalizes results
    /// </summary>
    private async Task<IEnumerable<HotelSearchResult>> SearchSingleProviderAsync(
        IHotelProvider provider,
        string destination,
        DateOnly checkIn,
        DateOnly checkOut,
        string? roomType,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Querying provider {ProviderId}: destination={Destination}",
                provider.ProviderId, destination);

            var providerResults = await provider.SearchAsync(
                destination, checkIn, checkOut, roomType, cancellationToken);

            var normalized = NormalizeResults(provider.ProviderId, providerResults);
            var count = normalized.Count();

            _logger.LogInformation(
                "Provider {ProviderId} returned {Count} results",
                provider.ProviderId, count);

            return normalized;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error querying provider {ProviderId}",
                provider.ProviderId);

            return Enumerable.Empty<HotelSearchResult>();
        }
    }

    /// <summary>
    /// Normalizes provider-specific results into unified DTO
    /// </summary>
    private static IEnumerable<HotelSearchResult> NormalizeResults(
        string providerId,
        IEnumerable<ProviderHotelResult> providerResults)
    {
        return providerResults.Select(result => new HotelSearchResult
        {
            HotelId = $"{providerId}-{result.HotelId}",
            HotelName = result.Name,
            Destination = "", // Set by caller if needed
            RoomType = result.RoomType,
            PricePerNight = result.PricePerNight,
            TotalPrice = result.PricePerNight * result.NumberOfNights,
            NumberOfNights = result.NumberOfNights,
            Rating = result.Rating,
            Amenities = result.Amenities,
            CancellationPolicy = NormalizeCancellationPolicy(result.CancellationPolicy),
            CancellationPolicyDetails = result.CancellationPolicyDetails,
            Provider = GetProviderDisplayName(providerId),
            Available = result.IsAvailable
        }).ToList();

        static string NormalizeCancellationPolicy(string policy)
        {
            return policy.ToLower() switch
            {
                "freecancellation" => "FreeCancellation",
                "free_cancellation" => "FreeCancellation",
                "flexible" => "Flexible",
                "non_refundable" => "NonRefundable",
                "nonrefundable" => "NonRefundable",
                _ => policy
            };
        }

        static string GetProviderDisplayName(string providerId)
        {
            return providerId.ToLower() switch
            {
                "premier-stays" => "PremierStays",
                "budget-nests" => "BudgetNests",
                "boutique-collection" => "BoutiqueCollection",
                _ => providerId
            };
        }
    }

    /// <summary>
    /// Property to indicate availability (for display purposes)
    /// </summary>
    private sealed class HotelSearchResultWithAvailable : HotelSearchResult
    {
        public bool Available { get; set; }
    }
}
