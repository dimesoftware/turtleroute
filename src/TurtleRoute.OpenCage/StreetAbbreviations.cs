using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace TurtleRoute.OpenCage
{
    /// <summary>
    /// Street abbreviations in the languages of a country, from OpenCage's address-formatting data (<see cref="AddressFormattingData"/>).
    /// "Dr" is "Drive" in Britain and "Dokter" in the Netherlands, so the country decides which apply.
    /// </summary>
    internal sealed class StreetAbbreviations
    {
        private static readonly ConcurrentDictionary<string, StreetAbbreviations> ByCountry = new(StringComparer.OrdinalIgnoreCase);

        private static readonly Regex Token = new(@"[\p{L}\p{N}]+(\.)?", RegexOptions.CultureInvariant);

        // Normalized single words, e.g. ("straat", "str"), with the full word as written for the query.
        private readonly (string Long, string Short, string Written)[] _pairs;

        private StreetAbbreviations(IEnumerable<string> languages)
            => _pairs = [.. languages
                .SelectMany(language => AddressFormattingData.RoadAbbreviations.TryGetValue(language, out var entries) ? entries : [])
                .Select(e => (Long: Words.Split(e.Long), Short: Words.Split(e.Short), Written: e.Long))
                // Single words only: "Zone industrielle: Z.I" does not map one word to one word.
                .Where(e => e.Long.Length == 1 && e.Short.Length == 1 && e.Long[0] != e.Short[0])
                .Select(e => (e.Long[0], e.Short[0], e.Written))];

        /// <summary>
        /// The abbreviations for a country (ISO 3166-1 alpha-2), or of all languages when the country is unknown.
        /// </summary>
        public static StreetAbbreviations For(string countryCode)
            => ByCountry.GetOrAdd(countryCode?.Trim() ?? "", code => new StreetAbbreviations(
                AddressFormattingData.CountryLanguages.TryGetValue(code.ToUpperInvariant(), out string[] languages) ? languages : AddressFormattingData.RoadAbbreviations.Keys));

        /// <summary>
        /// Whether two normalized words name the same: equal, a word and its abbreviation ("straat", "str"), or a compound and its abbreviation ("kerkstraat", "kerkstr").
        /// </summary>
        public bool Same(string a, string b)
            => a == b || _pairs.Any(p => Abbreviates(a, b, p.Long, p.Short) || Abbreviates(b, a, p.Long, p.Short));

        /// <summary>
        /// The text with abbreviations written out where they are unambiguous, e.g. "Avenue Louise 149" for "Av. Louise 149".
        /// An abbreviation with a vowel ("Av", "Burg") is only written out with its dot, as without it it may be an ordinary word ("Burg" is a castle in German).
        /// </summary>
        public string Expand(string text)
            => string.IsNullOrEmpty(text) ? text : Token.Replace(text, match =>
            {
                string word = match.Value.TrimEnd('.').ToLowerInvariant();
                if (!match.Groups[1].Success && word.Any(c => "aeiouy".Contains(c)))
                    return match.Value;

                string[] written = [.. _pairs.Where(p => p.Short == Words.Split(word).FirstOrDefault()).Select(p => p.Written).Distinct(StringComparer.OrdinalIgnoreCase)];
                return written.Length == 1 ? written[0] : match.Value;
            });

        // Whether "abbreviated" is "full" with its long form shortened: "str" for "straat", or "kerkstr" for "kerkstraat".
        // Compounds only for long forms of four letters or more, so a short one such as "kort" does not match inside names.
        private static bool Abbreviates(string full, string abbreviated, string longForm, string shortForm)
            => full == longForm
                ? abbreviated == shortForm
                : longForm.Length >= 4
                  && full.EndsWith(longForm, StringComparison.Ordinal) && abbreviated.EndsWith(shortForm, StringComparison.Ordinal)
                  && full.Length > longForm.Length
                  && full[..^longForm.Length] == abbreviated[..^shortForm.Length];
    }
}
