using System;
using System.Linq;

namespace TurtleRoute
{
    /// <summary>
    /// A structured postal address.
    /// </summary>
    public class Address
    {
        public Address(string street, string streetNo, string zipCode, string city, string state, string country)
        {
            if (string.IsNullOrWhiteSpace(street) && string.IsNullOrWhiteSpace(city) && string.IsNullOrWhiteSpace(country))
                throw new ArgumentException("Provide at least street, city or country.");

            Street = street;
            StreetNo = streetNo;
            ZipCode = zipCode;
            City = city;
            State = state;
            Country = country;
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
    }
}
