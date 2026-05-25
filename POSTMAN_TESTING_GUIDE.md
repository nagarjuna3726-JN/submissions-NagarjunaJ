# Postman Collection for HotelStay API

## Quick Import

1. Open Postman
2. Click **Import** → **Raw text** → Copy the JSON below
3. Click **Import**

## Environment Variables

Create a Postman Environment:
```json
{
  "base_url": "https://localhost:5001",
  "check_in": "2025-06-10",
  "check_out": "2025-06-12"
}
```

## API Endpoints

### 1. Health Check
```bash
GET {{base_url}}/health
```

### 2. Search Hotels
```bash
GET {{base_url}}/hotels/search?destination=London&checkIn=2025-06-10&checkOut=2025-06-12&roomType=Deluxe
```

**Query Parameters:**
- `destination` (required): City name
- `checkIn` (required): yyyy-MM-dd format
- `checkOut` (required): yyyy-MM-dd format
- `roomType` (optional): Standard, Deluxe, Suite

### 3. Book Hotel
```bash
POST {{base_url}}/hotels/book
Content-Type: application/json

{
  "hotelId": "premier-stays-grand-plaza-001",
  "destination": "London",
  "roomType": "Deluxe",
  "checkIn": "2025-06-10",
  "checkOut": "2025-06-12",
  "passengerName": "John Doe",
  "documentType": "Passport",
  "documentNumber": "AB123456"
}
```

### 4. Get Booking Status
```bash
GET {{base_url}}/hotels/booking/HLS-2025-06-10-001
```

## Test Scripts

### Bash
```bash
#!/bin/bash
BASE_URL="https://localhost:5001"

# 1. Health check
curl -s $BASE_URL/health | jq .

# 2. Search
curl -s "$BASE_URL/hotels/search?destination=London&checkIn=2025-06-10&checkOut=2025-06-12" | jq .

# 3. Book
curl -s -X POST $BASE_URL/hotels/book \
  -H "Content-Type: application/json" \
  -d '{
    "hotelId": "premier-stays-grand-plaza-001",
    "destination": "London",
    "roomType": "Deluxe",
    "checkIn": "2025-06-10",
    "checkOut": "2025-06-12",
    "passengerName": "John Doe",
    "documentType": "Passport",
    "documentNumber": "AB123456"
  }' | jq .

# 4. Get booking status
curl -s $BASE_URL/hotels/booking/HLS-2025-06-10-001 | jq .
```

### PowerShell
```powershell
$BaseUrl = "https://localhost:5001"

# 1. Health check
Invoke-RestMethod "$BaseUrl/health"

# 2. Search
Invoke-RestMethod "$BaseUrl/hotels/search?destination=London&checkIn=2025-06-10&checkOut=2025-06-12"

# 3. Book
$body = @{
    hotelId = "premier-stays-grand-plaza-001"
    destination = "London"
    roomType = "Deluxe"
    checkIn = "2025-06-10"
    checkOut = "2025-06-12"
    passengerName = "John Doe"
    documentType = "Passport"
    documentNumber = "AB123456"
} | ConvertTo-Json

Invoke-RestMethod "$BaseUrl/hotels/book" -Method POST -Body $body -ContentType "application/json"

# 4. Get booking
Invoke-RestMethod "$BaseUrl/hotels/booking/HLS-2025-06-10-001"
```

## Error Response Examples

### 400 Bad Request
```json
{
  "error": "destination is required",
  "statusCode": 400
}
```

### 422 Unprocessable Entity (Document Validation)
```json
{
  "error": "International destination London requires Passport",
  "statusCode": 422
}
```

### 404 Not Found
```json
{
  "error": "Booking not found",
  "statusCode": 404
}
```

## Testing Checklist

- [ ] Health endpoint responds
- [ ] Search returns results
- [ ] Search filters by room type
- [ ] Booking with Passport (international)
- [ ] Booking with National ID (domestic)
- [ ] Booking rejects invalid document
- [ ] Booking status retrieves data
- [ ] All error responses correct
- [ ] Database persists bookings

## Full Postman JSON

Download or import this collection directly in Postman.

[Collection JSON available in repository]

## Swagger UI

Access interactive API docs:
- **URL:** `https://localhost:5001/swagger`
- **Features:** Try-it-out, schemas, examples
