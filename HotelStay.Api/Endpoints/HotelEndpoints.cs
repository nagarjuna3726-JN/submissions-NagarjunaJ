using HotelStay.Api.Models;
using HotelStay.Api.Providers;

namespace HotelStay.Api.Endpoints;

/// <summary>
/// Hotel search and booking endpoints
/// </summary>
public static class HotelEndpoints
{
    /// <summary>
    /// Maps all hotel-related endpoints
    /// </summary>
    public static void MapHotelEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/hotels")
            .WithName("Hotels")
            .WithOpenApi();

        group.MapGet("/search", SearchHotels)
            .WithName("SearchHotels")
            .WithDescription("Search for available hotels by destination, dates, and room type")
            .WithOpenApi()
            .Produces<IEnumerable<HotelSearchResult>>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        group.MapPost("/book", BookHotel)
            .WithName("BookHotel")
            .WithDescription("Create a new hotel booking")
            .WithOpenApi()
            .Produces<BookingResponse>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ErrorResponse>(StatusCodes.Status422UnprocessableEntity)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
            .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        group.MapGet("/booking/{reference}", GetBookingStatus)
            .WithName("GetBookingStatus")
            .WithDescription("Retrieve booking status by reference number")
            .WithOpenApi()
            .Produces<BookingResponse>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
            .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);
    }

    private static async Task<IResult> SearchHotels(
        [FromQuery] string? destination,
        [FromQuery] string? checkIn,
        [FromQuery] string? checkOut,
        [FromQuery] string? roomType,
        IHotelAggregator aggregator,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(destination))
                return Results.BadRequest(new ErrorResponse { Error = "destination is required", StatusCode = 400 });

            if (string.IsNullOrWhiteSpace(checkIn))
                return Results.BadRequest(new ErrorResponse { Error = "checkIn is required (format: yyyy-MM-dd)", StatusCode = 400 });

            if (string.IsNullOrWhiteSpace(checkOut))
                return Results.BadRequest(new ErrorResponse { Error = "checkOut is required (format: yyyy-MM-dd)", StatusCode = 400 });

            if (!DateOnly.TryParse(checkIn, out var checkInDate))
                return Results.BadRequest(new ErrorResponse { Error = "Invalid checkIn date format (use yyyy-MM-dd)", StatusCode = 400 });

            if (!DateOnly.TryParse(checkOut, out var checkOutDate))
                return Results.BadRequest(new ErrorResponse { Error = "Invalid checkOut date format (use yyyy-MM-dd)", StatusCode = 400 });

            if (checkOutDate <= checkInDate)
                return Results.BadRequest(new ErrorResponse { Error = "checkOut date must be after checkIn date", StatusCode = 400 });

            var results = await aggregator.SearchHotelsAsync(destination, checkInDate, checkOutDate, roomType, cancellationToken);

            var resultsWithDestination = results.Select(r => new HotelSearchResult
            {
                HotelId = r.HotelId,
                HotelName = r.HotelName,
                Destination = destination,
                RoomType = r.RoomType,
                PricePerNight = r.PricePerNight,
                TotalPrice = r.TotalPrice,
                NumberOfNights = r.NumberOfNights,
                Rating = r.Rating,
                Amenities = r.Amenities,
                CancellationPolicy = r.CancellationPolicy,
                CancellationPolicyDetails = r.CancellationPolicyDetails,
                Provider = r.Provider
            }).ToList();

            logger.LogInformation("Hotel search handled: destination={Destination}, resultCount={Count}", destination, resultsWithDestination.Count);
            return Results.Ok(resultsWithDestination);
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("Hotel search was cancelled");
            return Results.StatusCode(StatusCodes.Status408RequestTimeout);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error during hotel search");
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    private static async Task<IResult> BookHotel(
        BookingRequest request,
        IDocumentValidator documentValidator,
        IBookingService bookingService,
        IHotelAggregator aggregator,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            if (request == null || string.IsNullOrWhiteSpace(request.HotelId))
                return Results.BadRequest(new ErrorResponse { Error = "hotelId is required", StatusCode = 400 });

            if (string.IsNullOrWhiteSpace(request.PassengerName))
                return Results.BadRequest(new ErrorResponse { Error = "passengerName is required", StatusCode = 400 });

            if (string.IsNullOrWhiteSpace(request.DocumentType))
                return Results.BadRequest(new ErrorResponse { Error = "documentType is required", StatusCode = 400 });

            if (string.IsNullOrWhiteSpace(request.DocumentNumber))
                return Results.BadRequest(new ErrorResponse { Error = "documentNumber is required", StatusCode = 400 });

            if (!DateOnly.TryParse(request.CheckIn, out var checkInDate) || !DateOnly.TryParse(request.CheckOut, out var checkOutDate))
                return Results.BadRequest(new ErrorResponse { Error = "Invalid date format", StatusCode = 400 });

            var (isDocumentValid, documentError) = await documentValidator.ValidateAsync(request.Destination, request.DocumentType, request.DocumentNumber);

            if (!isDocumentValid)
                return Results.UnprocessableEntity(new ErrorResponse { Error = documentError ?? "Document validation failed", StatusCode = 422 });

            var searchResults = await aggregator.SearchHotelsAsync(request.Destination, checkInDate, checkOutDate, request.RoomType, cancellationToken);

            var selectedHotel = searchResults.FirstOrDefault(h => h.HotelId == request.HotelId);
            if (selectedHotel == null)
                return Results.NotFound(new ErrorResponse { Error = "Hotel not found", StatusCode = 404 });

            var booking = await bookingService.BookHotelAsync(request.HotelId, request.Destination, request.RoomType, checkInDate, checkOutDate, selectedHotel.TotalPrice, selectedHotel.CancellationPolicy, cancellationToken);

            logger.LogInformation("Booking created: referenceNumber={ReferenceNumber}", booking.ReferenceNumber);
            return Results.Ok(booking);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating booking");
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    private static async Task<IResult> GetBookingStatus(
        string reference,
        IBookingService bookingService,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(reference))
                return Results.BadRequest(new ErrorResponse { Error = "Reference is required", StatusCode = 400 });

            var booking = await bookingService.GetBookingAsync(reference);
            if (booking == null)
                return Results.NotFound(new ErrorResponse { Error = "Booking not found", StatusCode = 404 });

            logger.LogInformation("Booking status retrieved: referenceNumber={ReferenceNumber}", reference);
            return Results.Ok(booking);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving booking");
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
}
