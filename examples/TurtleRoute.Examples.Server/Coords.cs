using TurtleRoute;

public class Coords
{
    public double lat { get; set; }
    public double lng { get; set; }

    public GeoCoordinate ToCoordinate() => new GeoCoordinate(lat, lng);
}