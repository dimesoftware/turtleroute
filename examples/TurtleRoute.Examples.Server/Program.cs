using Microsoft.AspNetCore.Mvc;
using TurtleRoute;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/coordinates", async (string search, IConfiguration config) =>
{
    string apiKey = config["token"] ?? throw new InvalidOperationException("API key not configured.");
    Geocoder geocoder = new(apiKey);
    return await geocoder.GeocodeAsync(search, string.Empty);
})
.WithName("GetCoordinates")
.WithOpenApi();

app.MapGet("/route", async (string from, string to, IConfiguration config) =>
{
    string apiKey = config["token"] ?? throw new InvalidOperationException("API key not configured.");
    Geocoder geocoder = new(apiKey);
    GeoCoordinate? fromCoordinates = await geocoder.GeocodeAsync(from, string.Empty);
    GeoCoordinate? toCoordinates = await geocoder.GeocodeAsync(to, string.Empty);

    Router router = new(apiKey);
    return await router.GetRouteAsync(fromCoordinates.Value, toCoordinates.Value);
})
.WithName("GetRoute")
.WithOpenApi();

app.MapPost("/trip", async ([FromBody] IEnumerable<GeoCoordinateDto> coordinates, IConfiguration config) =>
{
    string apiKey = config["token"] ?? throw new InvalidOperationException("API key not configured.");
    Geocoder geocoder = new(apiKey);

    Router router = new(apiKey);
    return await router.GetTrip(null, coordinates.Select(x => (GeoCoordinate)x).ToArray());
})
.WithName("GetTrip")
.WithOpenApi();

app.MapFallbackToFile("/index.html");

app.Run();

public class GeoCoordinateDto
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }

    public static implicit operator GeoCoordinate(GeoCoordinateDto dto)
        => new(dto.Latitude, dto.Longitude);
}