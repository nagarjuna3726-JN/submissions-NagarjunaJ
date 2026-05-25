using HotelStay.Api.Endpoints;
using HotelStay.Api.Providers;
using HotelStay.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to DI container
builder.Services.AddScoped<IHotelProvider, PremierStaysProvider>();
builder.Services.AddScoped<IHotelProvider, BudgetNestsProvider>();
builder.Services.AddScoped<IHotelProvider, BoutiqueCollectionProvider>();

builder.Services.AddScoped<IHotelAggregator, HotelAggregator>();
builder.Services.AddScoped<IDocumentValidator, DocumentValidator>();
builder.Services.AddScoped<IBookingService, BookingService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddLogging();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");

app.MapHotelEndpoints();

app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
    .WithName("Health")
    .WithOpenApi();

app.Run();
