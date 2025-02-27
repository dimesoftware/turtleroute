using System;

namespace TurtleRoute
{
    public readonly struct GeoCoordinate : IEquatable<GeoCoordinate>
    {
        public double Latitude { get; }
        public double Longitude { get; }

        public GeoCoordinate(double latitude, double longitude)
        {
            Latitude = latitude;
            Longitude = longitude;
        }

        public override string ToString()
            => $"{Latitude},{Longitude}";

        public override bool Equals(Object other)
            => other is GeoCoordinate coordinate && Equals(coordinate);

        public bool Equals(GeoCoordinate other)
            => Latitude == other.Latitude && Longitude == other.Longitude;

        public override int GetHashCode()
            => Latitude.GetHashCode() ^ Longitude.GetHashCode();

        public static bool operator ==(GeoCoordinate left, GeoCoordinate right)
            => left.Equals(right);

        public static bool operator !=(GeoCoordinate left, GeoCoordinate right)
            => !(left == right);
    }
}