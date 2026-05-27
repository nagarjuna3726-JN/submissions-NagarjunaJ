# Advanced Patterns Implementation Guide

This document outlines the advanced architectural patterns and features added to the HotelStay API.

## 1. Generic Repository Pattern

The repository pattern abstracts data access operations and provides a consistent interface for CRUD operations.

### Structure
- **IRepository<T>**: Generic interface defining standard CRUD operations
- **IBookingRepository**: Specialized repository for booking-specific operations
- **BookingRepository**: Implementation using in-memory storage

### Key Features
- Generic CRUD operations (Create, Read, Update, Delete)
- Find operations with predicates
- Existence checking
- Clean abstraction from data sources

### Usage
```csharp
public class BookingService
{
    private readonly IUnitOfWork _unitOfWork;

    public async Task<Booking> GetBookingAsync(string reference)
    {
        return await _unitOfWork.Bookings.GetByReferenceAsync(reference);
    }
}
```

## 2. Unit of Work Pattern

The Unit of Work pattern coordinates multiple repositories and manages transactions.

### Structure
- **IUnitOfWork**: Interface defining the unit of work contract
- **UnitOfWork**: Implementation coordinating repositories

### Key Features
- Centralized repository access
- Transaction management (BeginTransaction, Commit, Rollback)
- Single SaveChanges call for consistency
- Proper disposal handling with IDisposable and IAsyncDisposable

### Usage
```csharp
using var unitOfWork = _unitOfWorkFactory.Create();
await unitOfWork.BeginTransactionAsync();
try
{
    await unitOfWork.Bookings.AddAsync(booking);
    await unitOfWork.SaveChangesAsync();
    await unitOfWork.CommitAsync();
}
catch
{
    await unitOfWork.RollbackAsync();
    throw;
}
```

## 3. Custom Exception Handling

Structured exception handling with custom exception types and middleware.

### Exception Hierarchy
- **ApiException**: Base exception with status code and error code
- **ValidationException**: For validation failures (400)
- **NotFoundException**: For missing resources (404)
- **RateLimitExceededException**: For rate limit violations (429)

### Exception Handling Middleware
- Centralized exception handling
- Consistent error response format
- Proper HTTP status code mapping
- Correlation ID support

### Usage
```csharp
if (!booking.IsValid)
{
    throw new ValidationException(
        "Booking validation failed",
        new Dictionary<string, string[]>
        {
            { "hotelId", new[] { "Hotel not found" } }
        });
}
```

## 4. Rate Limiting

Sliding window algorithm-based rate limiting to prevent abuse.

### Configuration
```json
{
  "RateLimiting": {
    "Enabled": true,
    "MaxRequests": 100,
    "WindowSizeInSeconds": 60,
    "EndpointLimits": {
      "/hotels/search": 50,
      "/hotels/book": 20
    }
  }
}
```

### Features
- Per-IP rate limiting
- Endpoint-specific limits
- Sliding window algorithm
- Retry-After header support

### Middleware Integration
- Automatic client IP detection (X-Forwarded-For support)
- Early rejection of over-limit requests
- Clear error responses

## 5. Redis Caching

Distributed caching with Redis for improved performance.

### Structure
- **ICacheService**: Generic cache interface
- **RedisCacheService**: Redis implementation
- **InMemoryCacheService**: Development fallback

### Features
- Generic get/set operations
- Automatic JSON serialization
- Configurable expiration
- Connection pooling with StackExchange.Redis

### Configuration
```json
{
  "ConnectionStrings": {
    "Redis": "localhost:6379"
  }
}
```

### Usage
```csharp
// Cache hotel search results
var cacheKey = $"hotels:{destination}:{checkIn}:{checkOut}";
var cached = await _cacheService.GetAsync<List<Hotel>>(cacheKey);

if (cached == null)
{
    cached = await _hotelAggregator.SearchHotelsAsync(...);
    await _cacheService.SetAsync(cacheKey, cached, TimeSpan.FromHours(1));
}
```

## 6. Enhanced Swagger Documentation

Improved OpenAPI/Swagger integration with better documentation.

### Features
- Detailed API information (title, version, description)
- Contact and license information
- Rate limit headers documentation
- XML comment support
- Endpoint-specific response documentation

### Response Headers Documentation
- `X-RateLimit-Limit`: Maximum requests per window
- `X-RateLimit-Remaining`: Requests remaining in window
- `X-RateLimit-Reset`: Unix timestamp of window reset
- `Retry-After`: Seconds to wait before retrying (429 responses)

## 7. Service Configuration

Central service configuration for dependency injection.

### ServiceCollectionExtensions
```csharp
builder.Services.AddDataAccessServices();
builder.Services.AddRateLimiting(configuration);
builder.Services.AddRedisCache(configuration);
```

## Integration in Program.cs

```csharp
// Add advanced patterns
builder.Services.AddDataAccessServices();
builder.Services.AddRateLimiting(builder.Configuration);
builder.Services.AddRedisCache(builder.Configuration);

// Add middleware
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<RateLimitingMiddleware>();
```

## Architecture Diagram

```
┌─────────────────────────────────────────┐
│         HTTP Request                     │
└──────────────────┬──────────────────────┘
                   │
        ┌──────────▼──────────┐
        │ Rate Limiting       │
        │ Middleware          │
        └──────────┬──────────┘
                   │
        ┌──────────▼──────────┐
        │ Exception Handling  │
        │ Middleware          │
        └──────────┬──────────┘
                   │
        ┌──────────▼──────────┐
        │ Endpoint Handler    │
        └──────────┬──────────┘
                   │
        ┌──────────▼──────────┐
        │ Service Layer       │
        └──────────┬──────────┘
                   │
        ┌──────────▼──────────┐
        │ Unit of Work        │
        └──────────┬──────────┘
                   │
        ┌──────────▼──────────┐
        │ Repositories        │
        ├─────────────────────┤
        │ - Booking Repo      │
        │ - Cache Service     │
        └─────────────────────┘
```

## Configuration Examples

### Use Only In-Memory Cache (Development)
```csharp
builder.Services.AddInMemoryCache();
```

### Use Redis Cache (Production)
```csharp
builder.Services.AddRedisCache(builder.Configuration);
// Requires: ConnectionStrings:Redis in appsettings.json
```

### Disable Rate Limiting
```json
{
  "RateLimiting": {
    "Enabled": false
  }
}
```

## Best Practices

1. **Always use Unit of Work for transactions**
   - Ensures data consistency
   - Simplifies error handling

2. **Cache strategically**
   - Cache immutable data
   - Use appropriate expiration times
   - Monitor cache hit rates

3. **Monitor rate limits**
   - Log 429 responses
   - Track clients exceeding limits
   - Adjust limits based on usage patterns

4. **Handle exceptions gracefully**
   - Use specific exception types
   - Provide meaningful error messages
   - Include error codes for client handling

5. **Configure environment-specific settings**
   - Use appsettings.Development.json for dev
   - Use appsettings.Production.json for prod
   - Keep secrets in secret manager or environment variables

## Future Enhancements

1. Add database persistence instead of in-memory storage
2. Implement distributed rate limiting (Redis-based)
3. Add caching invalidation strategies
4. Implement circuit breaker pattern for external API calls
5. Add performance monitoring and metrics
