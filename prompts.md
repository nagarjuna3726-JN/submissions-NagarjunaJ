# Copilot Prompts Used in HotelStay Development

This document logs the Copilot prompts used for code generation, refactoring, and documentation.

## 1. Backend Architecture & Models

### Prompt 1.1: Generate Data Transfer Objects

```
Generate .NET 8 data transfer object (DTO) classes for a hotel search and booking platform.

Requirements:
- HotelSearchResult: hotelId, hotelName, destination, roomType, pricePerNight, totalPrice, numberOfNights, rating, amenities[], cancellationPolicy, provider
- BookingRequest: hotelId, passengerName, documentType, documentNumber, destination, roomType, checkIn, checkOut
- BookingResponse: referenceNumber, status, hotelName, hotelId, destination, roomType, checkIn, checkOut, totalPrice, cancellationPolicy, bookingDate

Use nullable reference types, required properties, and validation attributes where appropriate.
Include XML documentation comments.
```

### Prompt 1.2: Design Provider Abstraction Layer

```
Design a .NET interface and implementation pattern for a hotel provider abstraction.

Context:
- Multiple providers (PremierStays, BudgetNests, BoutiqueCollection)
- Each has different JSON field naming (PascalCase vs snake_case)
- Some providers return availability as boolean, others always available
- Need unified normalization to a common DTO

Requirements:
1. IHotelProvider interface with SearchAsync method
2. Abstract response model for each provider
3. Normalization logic in a separate service
4. Dependency injection ready
5. Deterministic stub responses for testing

Provide:
- Interface definition
- One example implementation (PremierStays stub)
- Aggregator service pattern
```

## 2. Backend Implementation

### Prompt 2.1: Implement PremierStays Provider

```
Implement PremierStays hotel provider stub.

Requirements:
- Provider ID: "premier-stays"
- JSON response with PascalCase fields
- Always returns availability for all room types (Standard, Deluxe, Suite)
- Return 2-3 hotels per search
- Include amenities, star rating (3-5), cancellation policy
- Cancellation policies: "FreeCancellation" (48h) or "NonRefundable"
- Hardcoded representative data
- No external API calls

Example hotels:
- Grand Plaza: $250/night, PascalCase JSON
- Luxury Tower: $350/night

Implement IHotelProvider interface.
Use async/await pattern.
Handle parameterized search (destination, checkIn, checkOut, roomType).
```

### Prompt 2.2: Implement BudgetNests Provider

```
Implement BudgetNests hotel provider stub.

Requirements:
- Provider ID: "budget-nests"
- JSON response with snake_case fields
- May return available: false for some room types (filter these out)
- Return 2-3 hotels per search
- Minimal details: room_type, rate_per_night, available, cancellation_policy only
- Cancellation policies: "flexible" (24h) or "non_refundable"
- Budget pricing ($50-150/night)
- Hardcoded representative data

Example hotels:
- Budget Inn: $75/night
- Economy Suites: $95/night

Some hotels may have available=false for Standard rooms (must be filtered by aggregator).
Implement IHotelProvider interface.
```

### Prompt 2.3: Implement Hotel Aggregator Service

```
Implement HotelAggregator service that:

1. Takes collection of IHotelProvider implementations
2. Executes all providers in parallel (Task.WhenAll)
3. Normalizes each provider response to HotelSearchResult
4. Filters out results where availability = false
5. Combines all results into single list
6. Sorts by pricePerNight ascending
7. Returns IEnumerable<HotelSearchResult>

Handle errors:
- Provider throws exception → log error, skip provider, continue with others
- No results from any provider → return empty list

Logging:
- Log each provider query
- Log provider errors with details
- Log final result count and execution time

Use ILogger<HotelAggregator> for logging.
```

### Prompt 2.4: Implement Document Validator

```
Implement DocumentValidator service with these rules:

Domestic destinations (New York, Los Angeles, Chicago):
- Accepts documentType: "Passport" or "NationalID"
- At least one required

International destinations (London, Paris, Tokyo, Sydney, Dubai):
- Requires documentType: "Passport" only
- NationalID rejected for international

Method signature:
Task<(bool isValid, string? errorMessage)> ValidateAsync(
    string destination, 
    string documentType, 
    string documentNumber)

Return:
- (true, null) if valid
- (false, "error message") if invalid

Error messages must be clear: "International destination requires Passport"
```

### Prompt 2.5: Generate Search Endpoint

```
Generate .NET Minimal API endpoint for GET /hotels/search.

Requirements:
- Query parameters: destination, checkIn, checkOut, roomType (all required except roomType)
- Parse dates as DateOnly (yyyy-MM-dd format)
- Validate:
  - destination: required, non-empty
  - checkIn: required, valid date format
  - checkOut: required, valid date, after checkIn
- Call HotelAggregator.SearchAsync()
- Return 200 OK with HotelSearchResult[] JSON
- Return 400 Bad Request if validation fails
- Include OpenAPI/Swagger attributes
- Log search parameters and result count

Endpoint definition:
app.MapGet("/hotels/search", SearchHotels)
    .WithName("SearchHotels")
    .WithOpenApi();
```

### Prompt 2.6: Generate Booking Endpoint

```
Generate .NET Minimal API endpoint for POST /hotels/book.

Requirements:
- Accept BookingRequest JSON body
- Validate all required fields
- Call DocumentValidator.ValidateAsync()
- Return 422 Unprocessable Entity if document validation fails
- Route booking to correct provider based on hotelId prefix ("premier-", "budget-", etc)
- Generate reference number format: HLS-yyyy-MM-dd-NNN
- Return 200 OK with BookingResponse
- Return 400 Bad Request if input validation fails
- Return 404 Not Found if hotel not found
- Include comprehensive error handling
- Log booking attempt with passenger name and destination

Endpoint definition:
app.MapPost("/hotels/book", BookHotel)
    .WithName("BookHotel")
    .WithOpenApi();
```

### Prompt 2.7: Generate Booking Status Endpoint

```
Generate .NET Minimal API endpoint for GET /hotels/booking/{reference}.

Requirements:
- URL parameter: reference (booking reference number)
- Look up booking in in-memory store (Dictionary<string, BookingResponse>)
- Return 200 OK with booking details if found
- Return 404 Not Found with error message if not found
- Include OpenAPI attributes
- Log lookup attempt

Endpoint definition:
app.MapGet("/hotels/booking/{reference}", GetBookingStatus)
    .WithName("GetBookingStatus")
    .WithOpenApi();
```

## 3. Frontend Components

### Prompt 3.1: Generate Search Form Component (Angular)

```
Generate Angular search form component.

Requirements:
- Reactive forms (FormGroup)
- Fields:
  - destination (dropdown): ["New York", "Los Angeles", "Chicago", "London", "Paris", "Tokyo", "Sydney", "Dubai"]
  - checkIn (date picker): required, min today
  - checkOut (date picker): required, min checkIn + 1 day
  - roomType (dropdown): optional ["Standard", "Deluxe", "Suite"], default all
- Validation:
  - All fields required except roomType
  - checkOut > checkIn
  - Show error messages
- Submit button: calls onSearch()
- Clear button: resets form
- Reactive validation with CSS classes (error state styling)

Use Angular Material for UI components (optional).
Include form reset functionality.
```

### Prompt 3.2: Generate Results List Component (Angular)

```
Generate Angular component to display hotel search results.

Requirements:
- Display array of HotelSearchResult objects
- Show for each:
  - Provider badge (PremierStays | BudgetNests | BoutiqueCollection)
  - Hotel name, destination
  - Room type, price per night, total price
  - Star rating, amenities
  - Cancellation policy label
- Sorting:
  - Default: price ascending
  - Allow sort by total price (toggle asc/desc)
- Selection:
  - Select button for each hotel
  - Emits (output) selected hotel to parent
- Display message if no results
- Loading state during search

Input: @Input() hotels: HotelSearchResult[];
Input: @Input() isLoading: boolean;
Output: @Output() hotelSelected = new EventEmitter<HotelSearchResult>();
Output: @Output() sortChanged = new EventEmitter<string>();
```

### Prompt 3.3: Generate Booking Form Component (Angular)

```
Generate Angular booking form component.

Requirements:
- Reactive forms (FormGroup)
- Fields:
  - passengerName (text input): required, min 3 chars
  - documentType (dropdown): ["Passport", "NationalID"], required
  - documentNumber (text input): required, alphanumeric
- Display (read-only):
  - Selected hotel details (destination, roomType, checkIn, checkOut)
  - Total price
- Validation:
  - All fields required
  - Real-time validation feedback
  - Custom validator: documentType must be valid for destination
- Submit button: disabled if form invalid
- Cancel button: goes back to results

Input: @Input() selectedHotel: HotelSearchResult;
Input: @Input() domesticDestinations: string[];
Input: @Input() internationalDestinations: string[];
Output: @Output() bookingSubmitted = new EventEmitter<BookingRequest>();
Output: @Output() cancelled = new EventEmitter<void>();
```

### Prompt 3.4: Generate Hotel Service (Angular)

```
Generate Angular service for hotel API communication.

Requirements:
- HttpClientModule integration
- Methods:
  - searchHotels(destination, checkIn, checkOut, roomType): Observable<HotelSearchResult[]>
  - bookHotel(request: BookingRequest): Observable<BookingResponse>
  - getBookingStatus(reference: string): Observable<BookingResponse>
- Base URL from environment config
- Error handling:
  - HTTP errors → throw with user-friendly message
  - 422 validation errors → return error details
  - Network errors → generic error message
- Logging: log requests and responses

Example:
export class HotelService {
  constructor(private http: HttpClient) {}
  
  searchHotels(...): Observable<HotelSearchResult[]> { ... }
}
```

## 4. Testing

### Prompt 4.1: Generate Unit Tests for Aggregator

```
Generate xUnit tests for HotelAggregator service.

Test cases:
1. SearchAsync returns combined results from all providers
2. SearchAsync filters out unavailable hotels
3. SearchAsync sorts by price ascending
4. SearchAsync handles provider exception gracefully
5. SearchAsync returns empty if no providers available
6. SearchAsync logs search parameters and result count

Mocking:
- Use Moq to mock IHotelProvider implementations
- Use ILogger<HotelAggregator> mock for logging verification

Assertions:
- Result count matches expectations
- Results sorted correctly
- Error provider skipped, others included
- Logging called with correct parameters
```

### Prompt 4.2: Generate Unit Tests for Document Validator

```
Generate xUnit tests for DocumentValidator service.

Test cases - Domestic (New York, LA, Chicago):
1. Passport accepted
2. NationalID accepted
3. Both valid

Test cases - International (London, Paris, etc):
1. Passport accepted
2. NationalID rejected → error message
3. Empty documentType rejected

Test cases - Invalid input:
1. Unknown destination
2. Empty destination
3. Null inputs

Assertions:
- isValid boolean correct
- Error message clear when invalid
- All scenarios covered
```

## 5. Documentation

### Prompt 5.1: Generate OpenAPI Documentation

```
Generate OpenAPI/Swagger documentation for HotelStay API.

Endpoints:
1. GET /hotels/search - search hotels
2. POST /hotels/book - create booking
3. GET /hotels/booking/{reference} - get booking status

For each:
- Clear description
- Request parameters/body with types and validation rules
- Response examples (200, 400, 422, 404, 500)
- Required fields marked
- Enum values documented

Generate Swagger configuration for Program.cs.
```

## 6. Live Tweak: BoutiqueCollection Provider

### Prompt 6.1: Implement BoutiqueCollection Provider

```
Implement BoutiqueCollection hotel provider stub.

Constraints:
- MUST NOT modify IHotelProvider interface
- MUST NOT modify HotelAggregator logic
- MUST NOT modify existing providers
- Only add: new class + DI registration

Requirements:
- Provider ID: "boutique-collection"
- Boutique premium rates: base_rate + £15/night fee
- Supports only: Deluxe & Suite (no Standard)
- Returns availability as boolean per room type
- CancellationPolicy: FreeCancellation up to 72 hours
- 2-3 boutique properties
- Example:
  - "Elegance Suites": £250/night base + £15 fee = £265/night
  - "Luxury Retreat": £200/night base + £15 fee = £215/night

Implement IHotelProvider.
Return ProviderHotelResult compatible with existing aggregator.
```

### Prompt 6.2: Register BoutiqueCollection in DI

```
Add BoutiqueCollection provider to Program.cs dependency injection.

Code:
builder.Services.AddScoped<IHotelProvider, BoutiqueCollectionProvider>();

Position: After existing provider registrations
No changes to aggregator or other code.
```

## Copilot Usage Summary

| Task | Prompts | Lines Generated | Approval Needed |
|------|---------|-----------------|------------------|
| Models/DTOs | 1.1 | ~150 | Yes |
| Provider Abstraction | 1.2, 2.1-2.3 | ~400 | Yes |
| Aggregator | 2.3 | ~200 | Yes |
| Validators | 2.4 | ~100 | Yes |
| Endpoints | 2.5-2.7 | ~300 | Yes |
| Tests | 4.1-4.2 | ~400 | Yes |
| Frontend | 3.1-3.4 | ~600 | Yes |
| Docs | 5.1 | ~200 | Yes |
| **Total** | **19** | **~2,350** | — |

## Copilot Effectiveness

✅ **Strengths:**
- Rapid DTO/model generation with proper attributes
- Interface design guidance
- Endpoint scaffolding
- Test case generation
- Error handling patterns

⚠️ **Manual Review Required:**
- Business logic (validation rules)
- Database schema alignment
- Security considerations
- Performance implications
- Provider-specific field mappings

🔄 **Iteration Cycle:**
1. Generate with Copilot
2. Review for correctness
3. Adjust business logic
4. Refine with follow-up prompts
5. Test and verify

## Lessons Learned

1. **Be Specific**: Generic prompts → generic code. Detailed specs → better results.
2. **Provide Context**: Including field names, business rules helps Copilot understand intent.
3. **Verify Logic**: Copilot generates syntactically correct code but may miss business constraints.
4. **Test Early**: Generate tests alongside code to validate Copilot output.
5. **Document Decisions**: Explain why you accepted/modified Copilot suggestions.
