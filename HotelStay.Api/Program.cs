using HotelStay.Api.Endpoints;
using HotelStay.Api.Providers;
using HotelStay.Api.Services;
using HotelStay.Api.Config;

var builder = WebApplication.CreateBuilder(args);

// Add services to DI container
builder.Services.AddScoped<IHotelProvider, PremierStaysProvider>();
builder.Services.AddScoped<IHotelProvider, BudgetNestsProvider>();
builder.Services.AddScoped<IHotelProvider, BoutiqueCollectionProvider>();

builder.Services.AddScoped<IHotelAggregator, HotelAggregator>();
builder.Services.AddScoped<IDocumentValidator, DocumentValidator>();
builder.Services.AddScoped<IBookingService, BookingService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerConfiguration(); // Custom Swagger configuration
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
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "HotelStay API v1.0");
        options.RoutePrefix = "swagger"; // Swagger UI at /swagger
        options.DefaultModelsExpandDepth(2);
        options.DefaultModelExpandDepth(2);
        options.DisplayOperationId();
        options.DisplayRequestDuration();
        options.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
    });
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");

app.MapHotelEndpoints();

app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
    .WithName("Health")
    .WithOpenApi()
    .WithDescription("Health check endpoint");

app.Run();
