using HotelStay.Api.Models;
using HotelStay.Api.Providers;

namespace HotelStay.Api.Services;

/// <summary>
/// Manages hotel bookings
/// </summary>
public interface IBookingService
{
    /// <summary>
    /// Creates a new booking
    /// </summary>
    Task<BookingResponse> BookHotelAsync(
        string hotelId,
        string destination,
        string roomType,
        DateOnly checkIn,
        DateOnly checkOut,
        decimal totalPrice,
        string cancellationPolicy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves booking status by reference number
    /// </summary>
    Task<BookingResponse?> GetBookingAsync(string referenceNumber);
}

/// <summary>
/// Implementation of booking service
/// </summary>
public class BookingService : IBookingService
{
    private static readonly Dictionary<string, BookingResponse> _bookings = new();
    private readonly ILogger<BookingService> _logger;
    private static int _bookingCounter = 0;

    public BookingService(ILogger<BookingService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Creates a new booking and stores it in-memory
    /// Generates a unique reference number
    /// </summary>
    public Task<BookingResponse> BookHotelAsync(
        string hotelId,
        string destination,
        string roomType,
        DateOnly checkIn,
        DateOnly checkOut,
        decimal totalPrice,
        string cancellationPolicy,
        CancellationToken cancellationToken = default)
    {
        // Generate reference number: HLS-yyyy-MM-dd-NNN
        var referenceNumber = GenerateReferenceNumber();

        var booking = new BookingResponse
        {
            ReferenceNumber = referenceNumber,
            Status = "Confirmed",
            HotelId = hotelId,
            HotelName = ExtractHotelName(hotelId), // Extract from hotelId or lookup
            Destination = destination,
            RoomType = roomType,
            CheckIn = checkIn.ToString("yyyy-MM-dd"),
            CheckOut = checkOut.ToString("yyyy-MM-dd"),
            TotalPrice = totalPrice,
            CancellationPolicy = cancellationPolicy,
            BookingDate = DateTime.UtcNow
        };

        _bookings[referenceNumber] = booking;

        _logger.LogInformation(
            "Booking created: referenceNumber={ReferenceNumber}, hotelId={HotelId}, destination={Destination}",
            referenceNumber, hotelId, destination);

        return Task.FromResult(booking);
    }

    /// <summary>
    /// Retrieves booking status by reference number
    /// </summary>
    public Task<BookingResponse?> GetBookingAsync(string referenceNumber)
    {
        if (_bookings.TryGetValue(referenceNumber, out var booking))
        {
            _logger.LogInformation("Booking retrieved: referenceNumber={ReferenceNumber}", referenceNumber);
            return Task.FromResult((BookingResponse?)booking);
        }

        _logger.LogWarning("Booking not found: referenceNumber={ReferenceNumber}", referenceNumber);
        return Task.FromResult((BookingResponse?)null);
    }

    /// <summary>
    /// Generates a unique reference number
    /// Format: HLS-yyyy-MM-dd-NNN
    /// </summary>
    private static string GenerateReferenceNumber()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var sequence = Interlocked.Increment(ref _bookingCounter);
        return $"HLS-{today:yyyy-MM-dd}-{sequence:D3}";
    }

    /// <summary>
    /// Extracts hotel name from hotel ID
    /// In production, would query hotel database
    /// </summary>
    private static string ExtractHotelName(string hotelId)
    {
        // Simple mapping for demo - in production would query database
        return hotelId switch
        {
            _ when hotelId.Contains("grand-plaza") => "Grand Plaza Hotel",
            _ when hotelId.Contains("luxury-tower") => "Luxury Tower",
            _ when hotelId.Contains("budget-inn") => "Budget Inn",
            _ when hotelId.Contains("economy-suites") => "Economy Suites",
            _ when hotelId.Contains("elegance-suites") => "Elegance Suites",
            _ when hotelId.Contains("luxury-retreat") => "Luxury Retreat",
            _ => "Hotel"
        };
    }
}
