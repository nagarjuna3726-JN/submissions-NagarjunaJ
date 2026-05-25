FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src

# Copy project files
COPY ["HotelStay.Api/HotelStay.Api.csproj", "HotelStay.Api/"]
COPY ["HotelStay.Tests/HotelStay.Tests.csproj", "HotelStay.Tests/"]

# Restore dependencies
RUN dotnet restore "HotelStay.Api/HotelStay.Api.csproj"

# Copy source code
COPY . .

# Build
RUN dotnet build "HotelStay.Api/HotelStay.Api.csproj" -c Release -o /app/build

# Publish
FROM build AS publish
RUN dotnet publish "HotelStay.Api/HotelStay.Api.csproj" -c Release -o /app/publish

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:7.0
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

COPY --from=publish /app/publish .

# Expose ports
EXPOSE 5000 5001

# Health check
HEALTHCHECK --interval=10s --timeout=5s --retries=5 \
  CMD curl -f http://localhost:5000/health || exit 1

# Run application
ENTRYPOINT ["dotnet", "HotelStay.Api.dll"]
