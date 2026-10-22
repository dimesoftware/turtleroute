using Microsoft.AspNetCore.Mvc;
using TurtleRoute;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Geocoders are tried in registration order: OpenCage first (when configured), Azure Maps as the fallback.
TurtleRouteBuilder turtleRoute = builder.Services.AddTurtleRoute();

string? openCageToken = builder.Configuration["openCageToken"];
if (!string.IsNullOrWhiteSpace(openCageToken))
    turtleRoute.AddOpenCageGeocoder(o => o.ApiKey = openCageToken);

string azureToken = builder.Configuration["token"] ?? throw new InvalidOperationException("API key not configured.");
turtleRoute
    .AddAzureMapsGeocoder(o => o.ApiKey = azureToken)
    .AddAzureMapsRouting(o => o.ApiKey = azureToken);

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

app.MapGet("/coordinates", async (string search, IGeocoder geocoder) =>
{
    GeocodeResult? result = await geocoder.GeocodeAsync(search, string.Empty);
    return result?.Coordinate;
})
.WithName("GetCoordinates")
.WithOpenApi();

app.MapGet("/route", async (string from, string to, IGeocoder geocoder, IRoutePlanner router) =>
{
    GeocodeResult? fromResult = await geocoder.GeocodeAsync(from, string.Empty);
    GeocodeResult? toResult = await geocoder.GeocodeAsync(to, string.Empty);
    if (fromResult == null || toResult == null)
        return Results.NotFound();

    return Results.Ok(await router.GetRouteAsync(fromResult.Coordinate, toResult.Coordinate));
})
.WithName("GetRoute")
.WithOpenApi();

app.MapPost("/trip", async ([FromBody] IEnumerable<string> addresses, IGeocoder geocoder, IRoutePlanner router) =>
{
    List<GeoCoordinate> coordinates = [];

    foreach (string address in addresses)
    {
        GeocodeResult? result = await geocoder.GeocodeAsync(address, string.Empty);
        if (result != null)
            coordinates.Add(result.Coordinate);
    }

    return await router.GetRouteAsync(new RoutePlanOptions() { OptimizeWaypointOrder = true, RouteType = RouteType.Shortest }, coordinates.ToArray());
})
.WithName("GetTrip")
.WithOpenApi();

app.MapPost("/sample", async ([FromBody] IEnumerable<Coords> coordinates, IRoutePlanner router) =>
{
    return await router.GetRouteAsync(new RoutePlanOptions() { OptimizeWaypointOrder = true, RouteType = RouteType.Shortest }, coordinates.Select(x => x.ToCoordinate()).ToArray());
})
.WithName("GetSample")
.WithOpenApi();

app.MapFallbackToFile("/index.html");

app.Run();
