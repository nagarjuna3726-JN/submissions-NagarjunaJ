# HotelStay: Hotel Search & Booking Platform

## Overview

HotelStay is a comprehensive hotel search and booking platform built as part of the SkyRoute Travel Platform. It demonstrates enterprise-grade architecture with multi-provider aggregation, document validation, and seamless booking workflows.

## Key Features

✅ **Multi-Provider Hotel Search**
- Query PremierStays (full details, premium rates)
- Query BudgetNests (budget options, minimal details)
- Query BoutiqueCollection (luxury boutique properties)
- Unified results normalization across providers

✅ **Smart Filtering & Sorting**
- Automatic availability filtering
- Sortable by price, rating, amenities
- Room type mapping to unified enum

✅ **Document Validation**
- International destinations require Passport
- Domestic destinations accept National ID
- Both client and server-side validation

✅ **Booking Workflow**
- POST endpoint for hotel reservations
- Reference-based booking status tracking
- Provider-agnostic booking orchestration

✅ **Production-Ready Code**
- Comprehensive error handling
- Structured logging
- Input validation
- OpenAPI/Swagger documentation

## Project Structure

```
submissions/usecase-002/
├── README.md                    # This file
├── spec.md                      # Complete requirements specification
├── prompts.md                   # Copilot prompts used
├── reflection.md                # Architectural decisions & rationale
│
├── HotelStay.Api/              # .NET Minimal API Backend
│   ├── Program.cs              # DI configuration & endpoint mapping
│   ├── HotelStay.Api.csproj    # Project file
│   ├── Models/                 # Data Transfer Objects
│   │   ├── SearchRequest.cs
│   │   ├── SearchResult.cs
│   │   ├── BookingRequest.cs
│   │   └── BookingResponse.cs
│   ├── Providers/              # Hotel provider abstractions
│   │   ├── IHotelProvider.cs
│   │   ├── PremierStaysProvider.cs
│   │   ├── BudgetNestsProvider.cs
│   │   ├── BoutiqueCollectionProvider.cs
│   │   └── HotelAggregator.cs
│   ├── Validators/             # Validation logic
│   │   ├── DocumentValidator.cs
│   │   └── SearchValidator.cs
│   └── Endpoints/              # Minimal API endpoints
│       ├── SearchEndpoint.cs
│       └── BookingEndpoint.cs
│
├── HotelStay.Tests/            # Unit & Integration Tests
│   ├── HotelStay.Tests.csproj
│   ├── Providers/
│   │   ├── PremierStaysProviderTests.cs
│   │   ├── BudgetNestsProviderTests.cs
│   │   └── HotelAggregatorTests.cs
│   └── Validators/
│       └── DocumentValidatorTests.cs
│
└── hotelstay-ui/               # Angular/React Frontend
    ├── README.md
    ├── package.json
    ├── src/
    │   ├── app/
    │   │   ├── components/
    │   │   │   ├── search-form/
    │   │   │   ├── results-list/
    │   │   │   └── booking-form/
    │   │   ├── services/
    │   │   │   └── hotel.service.ts
    │   │   ├── models/
    │   │   │   └── hotel.models.ts
    │   │   └── app.component.ts
    │   └── main.ts
    └── angular.json
```

## Quick Start

### Prerequisites
- .NET 8 SDK
- Node.js 18+
- Angular CLI (if using Angular frontend)

### Backend Setup

```bash
cd HotelStay.Api
dotnet restore
dotnet run
```

API will be available at `https://localhost:5001`
Swagger UI: `https://localhost:5001/swagger/index.html`

### Frontend Setup

```bash
cd hotelstay-ui
npm install
ng serve
# or
npm start
```

UI will be available at `http://localhost:4200`

## API Endpoints

### Search Hotels
```http
GET /hotels/search?destination={city}&checkIn={date}&checkOut={date}&roomType={type}
```

**Query Parameters:**
- `destination` (required): City name
- `checkIn` (required): ISO 8601 date (yyyy-MM-dd)
- `checkOut` (required): ISO 8601 date (yyyy-MM-dd)
- `roomType` (optional): Standard | Deluxe | Suite

**Response (200 OK):**
```json
[
  {
    "hotelId": "premier-001",
    "hotelName": "Grand Plaza",
    "destination": "New York",
    "roomType": "Deluxe",
    "pricePerNight": 250,
    "totalPrice": 1000,
    "numberOfNights": 4,
    "rating": 4.8,
    "amenities": ["WiFi", "Pool", "Gym"],
    "cancellationPolicy": "FreeCancellation",
    "provider": "PremierStays"
  }
]
```

### Book Hotel
```http
POST /hotels/book
Content-Type: application/json

{
  "hotelId": "premier-001",
  "passengerName": "John Doe",
  "documentType": "Passport",
  "documentNumber": "AB123456",
  "destination": "New York",
  "roomType": "Deluxe",
  "checkIn": "2026-06-01",
  "checkOut": "2026-06-05"
}
```

**Response (200 OK):**
```json
{
  "referenceNumber": "HLS-2026-05-25-001",
  "status": "Confirmed",
  "hotelName": "Grand Plaza",
  "totalPrice": 1000,
  "cancellationPolicy": "FreeCancellation",
  "bookingDate": "2026-05-25T13:45:00Z"
}
```

### Get Booking Status
```http
GET /hotels/booking/{reference}
```

**Response (200 OK):**
```json
{
  "referenceNumber": "HLS-2026-05-25-001",
  "status": "Confirmed",
  "hotelName": "Grand Plaza",
  "checkIn": "2026-06-01",
  "checkOut": "2026-06-05"
}
```

## Supported Destinations

### Domestic (National ID Accepted)
- New York
- Los Angeles
- Chicago

### International (Passport Required)
- London (UK)
- Paris (France)
- Tokyo (Japan)
- Sydney (Australia)
- Dubai (UAE)

## Room Types

- **Standard**: Basic room, essential amenities
- **Deluxe**: Upgraded room, enhanced amenities
- **Suite**: Premium room, full facilities

## Provider Integration

### PremierStays
- Premium rates
- Full property details
- Cancellation policies: FreeCancellation (48h) | NonRefundable
- All room types available

### BudgetNests
- Budget rates
- Minimal details
- Cancellation policies: Flexible (24h) | NonRefundable
- Selective room type availability

### BoutiqueCollection
- Boutique premium rates (+£15/night fee)
- Limited to Deluxe & Suite
- Cancellation policy: FreeCancellation (72h)

## Error Handling

**400 Bad Request**: Invalid input parameters
```json
{
  "error": "destination is required",
  "statusCode": 400
}
```

**422 Unprocessable Entity**: Document validation failed
```json
{
  "error": "International destination requires Passport",
  "statusCode": 422
}
```

**404 Not Found**: Booking reference not found
```json
{
  "error": "Booking reference not found",
  "statusCode": 404
}
```

**500 Internal Server Error**: Server-side error
```json
{
  "error": "An unexpected error occurred",
  "statusCode": 500
}
```

## Architecture Highlights

### Provider Abstraction
The `IHotelProvider` interface enables pluggable provider implementations without modifying aggregation logic. Each provider has deterministic, hardcoded responses for testing.

### Document Validation Strategy
Two-tier validation:
1. **Client-side** (Angular reactive forms): Immediate user feedback
2. **Server-side** (.NET validators): Security & consistency guarantee

### Booking Orchestration
Provider-agnostic booking engine routes to the correct provider based on `hotelId` prefix, supporting future provider additions seamlessly.

### Error Recovery
Provider failures don't break the entire search; other providers' results are still returned with comprehensive logging.

## Testing

Run unit tests:
```bash
cd HotelStay.Tests
dotnet test
```

Tests cover:
- Provider response normalization
- Document validation logic
- Search parameter validation
- Booking orchestration

## Copilot Usage

This project demonstrates effective Copilot usage for:
- **Code generation**: Entity models, DTOs, endpoints
- **Refactoring**: Provider abstraction layers
- **Documentation**: API specs, architectural decisions
- **Testing**: Unit test scaffolding

See `prompts.md` for specific Copilot prompts used.

## Deployment

### Backend (Azure App Service / Docker)
```bash
dotnet publish -c Release
# or
docker build -t hotelstay-api .
docker run -p 5001:80 hotelstay-api
```

### Frontend (Vercel / Azure Static Web Apps)
```bash
ng build --prod
# Deploy dist/ folder to CDN
```

## Future Enhancements

- [ ] Payment gateway integration
- [ ] Real-time availability updates (WebSocket)
- [ ] Multi-currency support
- [ ] User authentication & profiles
- [ ] Review system
- [ ] Loyalty program integration
- [ ] Mobile app (React Native)

## License

MIT

## Author

Nagarjuna J (@nagarjuna3726-JN)
