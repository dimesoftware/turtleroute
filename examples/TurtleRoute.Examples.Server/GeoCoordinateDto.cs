using TurtleRoute;

public class GeoCoordinateDto
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }

    public static implicit operator GeoCoordinate(GeoCoordinateDto dto)
        => new(dto.Latitude, dto.Longitude);
}