using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TurtleRoute.Tests
{
    [TestClass]
    public class GeocoderTests
    {
        private string _token;

        [TestInitialize]
        public void Setup()
        {
            IConfigurationRoot settings = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", false)
                .AddJsonFile("appsettings.development.json", true)
                .AddJsonFile("appsettings.test.json", true)
                .Build();

            foreach ((string key, string value) in settings.AsEnumerable())
            {
                switch (key)
                {
                    case "token":
                        _token = value;
                        break;

                    default:
                        continue;
                }
            }
        }

        [TestMethod]
        public void Geocoding_Constructor_InvalidParameter_Token_ThrowsArgumentNullException()
            => Assert.Throws<ArgumentNullException>(() => new Geocoder(string.Empty));

        [TestMethod]
        public async Task Geocoding_GetAddress_CountryInISO2_ShouldReturnCorrectCoordinates()
        {
            Geocoder api = new(_token);
            GeoCoordinate? address = await api.GeocodeAsync("Katwilgweg", "2", "2050", "Antwerpen", "", "BE");

            AssertCoordinates(address.GetValueOrDefault(), 4.359231, 51.219501);
        }

        [TestMethod]
        public async Task Geocoding_GetAddress_CountryInISO3_ShouldReturnCorrectCoordinates()
        {
            Geocoder api = new(_token);
            GeoCoordinate? address = await api.GeocodeAsync("Katwilgweg", "2", "2050", "Antwerpen", "", "BEL");

            AssertCoordinates(address.GetValueOrDefault(), 4.359231, 51.219501);
        }

        [TestMethod]
        public async Task Geocoding_GetAddress_CountryInEnglish_ShouldReturnCorrectCoordinates()
        {
            Geocoder api = new(_token);
            GeoCoordinate? address = await api.GeocodeAsync("Katwilgweg", "2", "2050", "Antwerpen", "", "Belgium");

            AssertCoordinates(address.GetValueOrDefault(), 4.359231, 51.219501);
        }

        [TestMethod]
        [DataRow("Maschstraße - K36, P. Nr. 1718067, 32120, Hiddenhausen", "DE", 8.615481, 52.169262)]
        [DataRow("1 Avenue du Château, 62124, Vélu", "FR", 2.972791, 50.104375)]
        [DataRow("Vélu, 1 Avenue du Château, 62124", "FR", 2.972791, 50.104375)]
        [DataRow("1 Avenue du Château, Vélu, 62124", "FR", 2.972791, 50.104375)]
        public async Task Geocoding_GetAddressByText_ShouldReturnCorrectCoordinates(string address, string country, double x, double y)
        {
            Geocoder api = new(_token);
            GeoCoordinate? res = await api.GeocodeAsync(address, country);

            AssertCoordinates(res.GetValueOrDefault(), x, y);
        }

        [TestMethod]
        [DataRow("Eenstraatdienietbestaat", "56", "9000", "Sjakkamakka", "BE")]
        [DataRow("Heilig Sakramentsraat", "26", "10008", "Zakkemakke", "DE")]
        public async Task GeocodeAsync_WrongAddress_ShouldReturnNull(string street, string streetNo, string zipCode, string city, string country)
        {
            Geocoder api = new(_token);
            GeoCoordinate? address = await api.GeocodeAsync(street, streetNo, zipCode, city, string.Empty, country);

            Assert.IsNull(address);
        }

        [TestMethod]
        public async Task GeocodeAsyncRaw_WrongAddress_ShouldReturnNull()
        {
            Geocoder api = new(_token);
            GeoCoordinate? address = await api.GeocodeAsync("Eenstraatdienietbestaat 56, , 9000, Sjakkamakka", "GB");

            Assert.IsNull(address);
        }

        private void AssertCoordinates(GeoCoordinate address, double x, double y)
            => Assert.IsTrue(GetDistance(address.Longitude, address.Latitude, x, y) <= 1);

        private double GetDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const int radius = 6371; // Radius of the earth in km
            double dLat = ToRadians(lat2 - lat1);
            double dLon = ToRadians(lon2 - lon1);
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) + Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            double d = radius * c;  // Distance in km
            return d;
        }

        private double ToRadians(double deg) => deg * (Math.PI / 180);
    }
}