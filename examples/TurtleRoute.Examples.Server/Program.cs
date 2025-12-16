using Azure.Maps.Routing;
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

app.MapPost("/trip", async ([FromBody] IEnumerable<string> addresses, IConfiguration config) =>
{
    string apiKey = config["token"] ?? throw new InvalidOperationException("API key not configured.");

    List<GeoCoordinate> coordinates = [];
    Geocoder geocoder = new(apiKey);

    foreach (string address in addresses)
    {
        GeoCoordinate? coordinate = await geocoder.GeocodeAsync(address, string.Empty);
        if (coordinate != null)
            coordinates.Add(coordinate.Value);
    }

    Router tripper = new(apiKey);
    return await tripper.GetRouteAsync(new() { ComputeBestWaypointOrder = true, RouteType = RouteType.Shortest }, coordinates.Select(x => x).ToArray());
})
.WithName("GetTrip")
.WithOpenApi();

app.MapPost("/sample", async ([FromBody] IEnumerable<Coords> coordinates, IConfiguration config) =>
{
    string apiKey = config["token"] ?? throw new InvalidOperationException("API key not configured.");

    Router tripper = new(apiKey);
    return await tripper.GetRouteAsync(new() { ComputeBestWaypointOrder = true, RouteType = RouteType.Shortest }, coordinates.Select(x => x.ToCoordinate()).ToArray());
})
.WithName("GetSample")
.WithOpenApi();

app.MapFallbackToFile("/index.html");

app.Run();
