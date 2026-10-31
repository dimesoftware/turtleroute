using System;
using System.Collections.Generic;
using System.Linq;

namespace TurtleRoute
{
    /// <summary>
    /// A structured postal address.
    /// </summary>
    /// <remarks>
    /// Line breaks become ", " and placeholders such as "undefined", "-" or "XXXX" become null, as geocoders would search for them.
    /// </remarks>
    public class Address
    {
        private static readonly HashSet<string> Placeholders = new(["undefined", "null", "none", "nan", "missing"], StringComparer.OrdinalIgnoreCase);

        public Address(string street, string streetNo, string zipCode, string city, string state, string country)
        {
            if (string.IsNullOrWhiteSpace(street) && string.IsNullOrWhiteSpace(city) && string.IsNullOrWhiteSpace(country))
                throw new ArgumentException("Provide at least street, city or country.");

            Street = Clean(street);
            StreetNo = Clean(streetNo);
            ZipCode = Clean(zipCode);
            City = Clean(city);
            State = Clean(state);
            Country = Clean(country);
        }

        public string Street { get; }
        public string StreetNo { get; }
        public string ZipCode { get; }
        public string City { get; }
        public string State { get; }
        public string Country { get; }

        /// <summary>
        /// The non-empty address parts joined by commas, e.g. "Katwilgweg, 2, 2050, Antwerpen, BE".
        /// </summary>
        public override string ToString()
            => string.Join(", ", new[] { Street, StreetNo, ZipCode, City, State, Country }.Where(p => !string.IsNullOrWhiteSpace(p)));

        private static string Clean(string value)
        {
            if (value == null)
                return null;

            value = string.Join(", ", value.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
            return !value.Any(char.IsLetterOrDigit) || Placeholders.Contains(value) || value.All(c => c is 'x' or 'X') ? null : value;
        }
    }
}
