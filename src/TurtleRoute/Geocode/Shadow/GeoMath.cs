using System;

namespace TurtleRoute
{
    internal static class GeoMath
    {
        private const double EarthRadiusInMeters = 6371000;

        /// <summary>
        /// The great-circle (haversine) distance between two coordinates.
        /// </summary>
        public static double DistanceInMeters(GeoCoordinate from, GeoCoordinate to)
        {
            double dLat = ToRadians(to.Latitude - from.Latitude);
            double dLon = ToRadians(to.Longitude - from.Longitude);
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                + Math.Cos(ToRadians(from.Latitude)) * Math.Cos(ToRadians(to.Latitude)) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            return EarthRadiusInMeters * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        }

        private static double ToRadians(double degrees) => degrees * (Math.PI / 180);
    }
}
