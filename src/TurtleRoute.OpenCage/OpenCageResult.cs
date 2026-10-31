using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TurtleRoute.OpenCage
{
    internal class OpenCageResult
    {
        // The city, hamlet, municipality and neighbourhood aliases in OpenCage's components.yaml.
        // No state or county: "Broadway 1000, New York" would match Rochester, NY.
        private static readonly string[] LocalityKeys =
            ["_normalized_city", "city", "town", "township", "village", "hamlet", "locality", "croft", "municipality", "local_administrative_area", "area", "subcounty",
             "neighbourhood", "suburb", "city_district", "district", "subdistrict", "commune", "quarter", "borough", "ward", "city_block", "postal_city"];

        // "road" and its aliases in OpenCage's address-formatting components.yaml.
        private static readonly string[] StreetKeys = ["road", "street", "street_name", "square", "pedestrian", "footway", "path", "road_reference", "road_reference_intl"];

        private static readonly string[] HouseNumberKeys = ["house_number", "street_number", "housenumber"];

        private static readonly string[] RegionKeys = ["state", "state_code", "county", "country", "country_code"];

        /// <summary>
        /// 0-10: how small the bounding box of the match is. 10 is under 250 m, 7 under 5 km, 1 over 25 km, 0 unknown.
        /// </summary>
        [JsonPropertyName("confidence")]
        public int Confidence { get; set; }

        [JsonPropertyName("geometry")]
        public OpenCageGeometry Geometry { get; set; }

        [JsonPropertyName("formatted")]
        public string Formatted { get; set; }

        [JsonPropertyName("components")]
        public Dictionary<string, JsonElement> Components { get; set; } = [];

        [JsonIgnore]
        public string Type => Get("_type");

        [JsonIgnore]
        public string Street => StreetKeys.Select(Get).FirstOrDefault(street => !string.IsNullOrWhiteSpace(street));

        [JsonIgnore]
        public string HouseNumber => HouseNumberKeys.Select(Get).FirstOrDefault(number => !string.IsNullOrWhiteSpace(number));

        [JsonIgnore]
        public string Postcode => Get("postcode");

        private IEnumerable<string[]> StreetNames => Names(Street);

        private IEnumerable<string[]> Localities => LocalityKeys.SelectMany(key => Names(Get(key)));

        /// <summary>
        /// Whether the match is on the requested street and in the requested postcode or city.
        /// </summary>
        public bool Matches(Address address)
        {
            string[] zip = Words.Split(address.ZipCode);
            string[] postcode = Words.Split(Postcode);

            // OpenCage's fallback when it cannot find the street; ToConfidence decides whether it is precise enough.
            if (Type == "postcode")
                return zip.Length > 0 && string.Concat(zip) == string.Concat(postcode);

            // The street field often holds more than the street, e.g. "Suite 400, 1000 Broadway".
            // In the countryside it may name a hamlet instead, e.g. "Dünninghausen 10, 59269 Beckum".
            string[] street = Words.Split(address.Street);
            IEnumerable<string[]> places = Street != null ? StreetNames : Localities;
            if (street.Length > 0 && !places.Any(name => Words.IndexOf(street, name) >= 0))
                return false;

            if (zip.Length > 0 && postcode.Length > 0)
                return IsSamePostcode(zip, postcode);

            string[] city = Words.Split(address.City);
            if (city.Length == 0)
                return zip.Length == 0;

            return Localities.Any(locality => locality.SequenceEqual(city));
        }

        /// <summary>
        /// Whether the free-text query names the street and the city or postcode of the match, and nothing it did not find.
        /// </summary>
        public bool Matches(string query)
        {
            List<string> words = [.. Words.Split(query)];

            if (Type == "postcode")
                return RemovePostcode(words);

            if (Street != null && !StreetNames.Any(street => RemoveStreet(words, street)))
                return false;

            bool inLocality = false;
            foreach (string[] locality in Localities)
                inLocality |= Remove(words, locality);
            bool inPostcode = RemovePostcode(words);
            if (!inLocality && !inPostcode)
                return false;

            if (Street != null)
                return true;

            // Without a street the match is a city centre, so the query must not name anything else.
            foreach (string[] region in RegionKeys.SelectMany(key => Names(Get(key))))
                Remove(words, region);

            return words.All(word => word.All(char.IsDigit));
        }

        // Also removes the house number next to the street, so it is not taken for a postcode.
        private static bool RemoveStreet(List<string> words, string[] street)
        {
            int start = Words.IndexOf([.. words], street);
            if (start < 0)
                return false;

            int end = start + street.Length;
            if (end < words.Count && words[end].All(char.IsDigit))
                end++;
            else if (start > 0 && words[start - 1].All(char.IsDigit))
                start--;

            words.RemoveRange(start, end - start);
            return true;
        }

        // The whole postcode, "SW20 8LD" or "SW208LD": "SW20" alone is a district.
        private bool RemovePostcode(List<string> words)
        {
            string[] postcode = Words.Split(Postcode);
            return postcode.Length > 0 && (Remove(words, postcode) || Remove(words, [string.Concat(postcode)]));
        }

        private static bool Remove(List<string> words, string[] part)
        {
            int start = Words.IndexOf([.. words], part);
            if (start < 0)
                return false;

            words.RemoveRange(start, part.Length);
            return true;
        }

        // "3552" and "3552 XM" are the same postcode, with and without the street-level part.
        private static bool IsSamePostcode(string[] zip, string[] postcode)
            => string.Concat(zip) == string.Concat(postcode) || Words.StartsWith(postcode, zip) || Words.StartsWith(zip, postcode);

        // Bilingual names such as "Bruxelles - Brussel" match either language, and "Fürth" also matches "Fuerth".
        private static IEnumerable<string[]> Names(string value)
            => (value ?? "").Split([" - ", " / "], StringSplitOptions.RemoveEmptyEntries)
                .SelectMany(name => new[] { Words.Split(name), Words.Split(Words.ExpandUmlauts(name)) })
                .Where(name => name.Length > 0);

        private string Get(string key)
            => Components != null && Components.TryGetValue(key, out JsonElement value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
    }
}
