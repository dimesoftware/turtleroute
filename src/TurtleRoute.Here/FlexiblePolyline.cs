using System;
using System.Collections.Generic;

namespace TurtleRoute.Here
{
    /// <summary>
    /// Decodes HERE's flexible polyline format (https://github.com/heremaps/flexible-polyline).
    /// </summary>
    internal static class FlexiblePolyline
    {
        private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_";

        public static List<GeoCoordinate> Decode(string encoded)
        {
            List<GeoCoordinate> coordinates = [];
            if (string.IsNullOrEmpty(encoded))
                return coordinates;

            int index = 0;

            long version = ReadUnsigned(encoded, ref index);
            if (version != 1)
                throw new FormatException($"Unsupported flexible polyline version {version}.");

            long header = ReadUnsigned(encoded, ref index);
            int precision = (int)(header & 15);
            bool hasThirdDimension = ((header >> 4) & 7) != 0;
            double factor = Math.Pow(10, precision);

            long latitude = 0;
            long longitude = 0;

            while (index < encoded.Length)
            {
                latitude += ReadSigned(encoded, ref index);
                longitude += ReadSigned(encoded, ref index);

                // Elevation, level or other third dimensions are not part of a GeoCoordinate.
                if (hasThirdDimension)
                    ReadSigned(encoded, ref index);

                coordinates.Add(new GeoCoordinate(latitude / factor, longitude / factor));
            }

            return coordinates;
        }

        private static long ReadUnsigned(string encoded, ref int index)
        {
            long result = 0;
            int shift = 0;

            while (true)
            {
                if (index >= encoded.Length)
                    throw new FormatException("Unexpected end of flexible polyline.");

                int value = Alphabet.IndexOf(encoded[index++]);
                if (value < 0)
                    throw new FormatException($"Invalid flexible polyline character '{encoded[index - 1]}'.");

                result |= (long)(value & 0x1F) << shift;
                if ((value & 0x20) == 0)
                    return result;

                shift += 5;
            }
        }

        private static long ReadSigned(string encoded, ref int index)
        {
            long value = ReadUnsigned(encoded, ref index);
            return (value & 1) != 0 ? ~(value >> 1) : value >> 1;
        }
    }
}
