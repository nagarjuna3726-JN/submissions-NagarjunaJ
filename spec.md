# HotelStay: Complete Specification

## Executive Summary

HotelStay is a hotel search and booking platform for the SkyRoute Travel Platform. Travellers search by destination, dates, and room type. The system queries multiple providers, normalises results, and presents a unified list. Upon selection, travellers complete a booking with document validation.

## Requirements

### Functional Requirements

#### 1. Hotel Search (Backend)

**Endpoint:** `GET /hotels/search`

**Input Parameters:**
- `destination` (string, required): City name
- `checkIn` (string, required): ISO 8601 date (yyyy-MM-dd)
- `checkOut` (string, required): ISO 8601 date (yyyy-MM-dd)
- `roomType` (string, optional): Standard | Deluxe | Suite

**Validation:**
- All fields must be present (except roomType)
- `checkOut` must be after `checkIn`
- `destination` must be in supported list
- Return 400 for invalid input

**Processing:**
1. Query all configured providers in parallel
2. Filter unavailable results (availability = false)
3. Normalise provider responses to unified DTO
4. Return sorted list (price ascending)
5. Handle provider failures gracefully (log, continue with others)

**Output:**
```json
[
  {
    "hotelId": "unique-id-per-provider",
    "hotelName": "string",
    "destination": "string",
    "roomType": "Standard|Deluxe|Suite",
    "pricePerNight": decimal,
    "totalPrice": decimal,
    "numberOfNights": int,
    "rating": decimal (0-5),
    "amenities": string[],
    "cancellationPolicy": "FreeCancellation|Flexible|NonRefundable",
    "provider": "PremierStays|BudgetNests|BoutiqueCollection"
  }
]
```

#### 2. Hotel Booking (Backend)

**Endpoint:** `POST /hotels/book`

**Input:**
```json
{
  "hotelId": "string (required)",
  "passengerName": "string (required)",
  "documentType": "Passport|NationalID (required)",
  "documentNumber": "string (required)",
  "destination": "string (required)",
  "roomType": "Standard|Deluxe|Suite (required)",
  "checkIn": "yyyy-MM-dd (required)",
  "checkOut": "yyyy-MM-dd (required)"
}
```

**Validation:**
- All fields required
- Document type must match destination:
  - International → Passport required
  - Domestic → Passport or National ID accepted
- Return 422 with error message if mismatch
- Server-side validation mandatory

**Processing:**
1. Validate all inputs
2. Validate document requirement
3. Route to appropriate provider (via hotelId prefix)
4. Execute booking with provider
5. Generate reference number
6. Store booking state (in-memory or DB)

**Output (200 OK):**
```json
{
  "referenceNumber": "HLS-YYYY-MM-DD-NNN",
  "status": "Confirmed",
  "hotelName": "string",
  "hotelId": "string",
  "destination": "string",
  "roomType": "string",
  "checkIn": "yyyy-MM-dd",
  "checkOut": "yyyy-MM-dd",
  "totalPrice": decimal,
  "cancellationPolicy": "string",
  "bookingDate": "ISO 8601 timestamp"
}
```

**Error Responses:**
- 400: Invalid input
- 422: Document validation failed
- 404: Hotel not found
- 500: Booking failed

#### 3. Booking Status (Backend)

**Endpoint:** `GET /hotels/booking/{reference}`

**Output (200 OK):**
```json
{
  "referenceNumber": "string",
  "status": "Confirmed|Pending|Cancelled",
  "hotelName": "string",
  "destination": "string",
  "roomType": "string",
  "checkIn": "yyyy-MM-dd",
  "checkOut": "yyyy-MM-dd",
  "totalPrice": decimal
}
```

**Error:** 404 if reference not found

#### 4. Frontend Features

**Search Form:**
- Destination dropdown (2 domestic + 3 international)
- Date pickers (checkIn, checkOut)
- Room type selector (optional, defaults to all)
- Search button
- Validation feedback on form submission

**Results List:**
- Provider badge (PremierStays | BudgetNests | BoutiqueCollection)
- Room type, per-night rate, total price
- Cancellation policy label
- Star rating, amenities
- Sortable by total price (ascending/descending)
- Select button for each result

**Booking Form:**
- Passenger name input
- Document type selector (Passport | National ID)
- Document number input
- Display destination and selected room details
- Submit button
- Real-time validation

**Booking Confirmation:**
- Reference number displayed
- Provider name
- Total price
- Cancellation policy
- Check-in/out dates
- Print / Download option (optional)

### Providers

#### PremierStays

**Characteristics:**
- Premium pricing
- Full property details
- JSON field naming: PascalCase

**Response Structure:**
```json
[
  {
    "HotelId": "string",
    "HotelName": "string",
    "Address": "string",
    "RoomType": "Standard|Deluxe|Suite",
    "RatePerNight": decimal,
    "Availability": true,
    "Amenities": ["WiFi", "Pool", "Gym"],
    "StarRating": decimal (0-5),
    "CancellationPolicy": "FreeCancellation|NonRefundable",
    "FreeCancellationHours": 48
  }
]
```

**Guarantees:**
- Always returns availability for all requested room types
- Deterministic responses (hardcoded stubs)

**Cancellation Policies:**
- `FreeCancellation`: Up to 48 hours before check-in
- `NonRefundable`: No refunds

#### BudgetNests

**Characteristics:**
- Budget pricing
- Minimal details (room type, rate, cancellation policy only)
- JSON field naming: snake_case

**Response Structure:**
```json
[
  {
    "hotel_id": "string",
    "hotel_name": "string",
    "room_type": "standard|deluxe|suite",
    "rate_per_night": decimal,
    "available": true|false,
    "cancellation_policy": "flexible|non_refundable",
    "cancellation_hours": 24
  }
]
```

**Guarantees:**
- May return `available: false` for some room types (must filter out)
- Deterministic responses (hardcoded stubs)

**Cancellation Policies:**
- `Flexible`: Up to 24 hours before check-in
- `NonRefundable`: No refunds

#### BoutiqueCollection (Live Tweak)

**Characteristics:**
- Boutique premium rates
- Base nightly rate + £15/night boutique fee
- Supports Deluxe & Suite only
- CancellationPolicy: FreeCancellation (up to 72h)
- Availability as boolean per room type

**Response Structure:**
```json
[
  {
    "boutique_id": "string",
    "boutique_name": "string",
    "room_type": "deluxe|suite",
    "base_rate": decimal,
    "boutique_fee": 15,
    "available": true|false
  }
]
```

**Implementation Constraints:**
- Add without modifying IHotelProvider interface
- Add without modifying HotelAggregator
- Add without modifying existing provider implementations
- Only add new implementation + DI registration

### Document Validation Rules

**Domestic Destinations:**
- New York, Los Angeles, Chicago
- Accepted documents: Passport, National ID
- Requirement: At least one valid document

**International Destinations:**
- London, Paris, Tokyo, Sydney, Dubai
- Required document: Passport only
- Requirement: Passport must be provided

**Validation Points:**
1. Client-side (React/Angular form): Real-time feedback
2. Server-side (.NET): Authority enforcement
3. Response code 422 if mismatch

### Non-Functional Requirements

#### Performance
- Provider queries in parallel (not sequential)
- Response time < 2 seconds for full search
- Provider failure tolerance (one provider down ≠ entire search fails)

#### Reliability
- Comprehensive logging (search queries, provider responses, errors)
- Structured error responses
- Graceful degradation if provider unavailable

#### Code Quality
- SOLID principles
- Dependency injection
- Unit tests (>80% coverage target)
- OpenAPI/Swagger documentation

#### Security
- Input validation (both client & server)
- No sensitive data in logs
- HTTPS only (in production)
- CORS configured appropriately

## Room Type Mapping

### PremierStays
- "Standard" → Standard
- "Deluxe" → Deluxe
- "Suite" → Suite

### BudgetNests
- "standard" → Standard
- "deluxe" → Deluxe
- "suite" → Suite

### BoutiqueCollection
- "deluxe" → Deluxe
- "suite" → Suite

## Cancellation Policy Mapping

### PremierStays
- "FreeCancellation" (48h) → FreeCancellation
- "NonRefundable" → NonRefundable

### BudgetNests
- "flexible" (24h) → Flexible
- "non_refundable" → NonRefundable

### BoutiqueCollection
- "free_cancellation" (72h) → FreeCancellation

## Testing Scenarios

### Scenario 1: Successful Search
- Input: destination=London, checkIn=2026-06-01, checkOut=2026-06-05, roomType=Deluxe
- Expected: Returns results from all 3 providers, sorted by price, all available

### Scenario 2: Filtered Unavailable
- Input: destination=Paris, checkIn=2026-06-10, checkOut=2026-06-15, roomType=Standard
- Expected: BudgetNests may return some unavailable; filtered out automatically

### Scenario 3: Document Validation - International
- Input: destination=Tokyo, documentType=NationalID
- Expected: 422 error "International destination requires Passport"

### Scenario 4: Document Validation - Domestic
- Input: destination=NewYork, documentType=NationalID
- Expected: 200 OK, booking confirmed

### Scenario 5: New Provider (BoutiqueCollection)
- Input: Add BoutiqueCollection provider
- Expected: Search results include boutique properties without code changes
- Constraint: No changes to existing code

### Scenario 6: Provider Failure
- Input: PremierStays provider offline
- Expected: Search returns results from BudgetNests & BoutiqueCollection
- No 500 error, graceful degradation

## Database Model (if applicable)

```sql
CREATE TABLE Bookings (
    Id UUID PRIMARY KEY,
    ReferenceNumber NVARCHAR(50) UNIQUE NOT NULL,
    HotelId NVARCHAR(100) NOT NULL,
    HotelName NVARCHAR(255) NOT NULL,
    PassengerName NVARCHAR(255) NOT NULL,
    DocumentType NVARCHAR(50) NOT NULL,
    DocumentNumber NVARCHAR(100) NOT NULL,
    Destination NVARCHAR(100) NOT NULL,
    RoomType NVARCHAR(50) NOT NULL,
    CheckInDate DATE NOT NULL,
    CheckOutDate DATE NOT NULL,
    TotalPrice DECIMAL(10,2) NOT NULL,
    CancellationPolicy NVARCHAR(50),
    Provider NVARCHAR(50) NOT NULL,
    Status NVARCHAR(50) NOT NULL DEFAULT 'Confirmed',
    BookingDate DATETIME NOT NULL DEFAULT GETUTCDATE(),
    CreatedAt DATETIME NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME NOT NULL DEFAULT GETUTCDATE()
);

CREATE INDEX IX_Bookings_ReferenceNumber ON Bookings(ReferenceNumber);
CREATE INDEX IX_Bookings_PassengerName ON Bookings(PassengerName);
```

## Deployment Configuration

### Environment Variables

```env
# Backend
APISETTINGS__LOGPATH=/var/log/hotelstay
APISETTINGS__ENVIRONMENT=Production
CONNECTIONSTRINGS__DEFAULT=Server=...;Database=hotelstay

# Frontend
NG_API_URL=https://api.hotelstay.com
NG_ENVIRONMENT=production
```

## Acceptance Criteria

- [x] All 3 providers return results
- [x] Results normalized to unified DTO
- [x] Unavailable results filtered
- [x] Sorting by price works
- [x] Document validation enforced (client & server)
- [x] Booking created successfully
- [x] Reference number retrievable
- [x] Error handling comprehensive
- [x] Logging includes search/booking details
- [x] BoutiqueCollection added without modifying existing code
- [x] All 5 destinations supported
- [x] Unit tests written
- [x] API documentation complete
