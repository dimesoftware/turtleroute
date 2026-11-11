using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace TurtleRoute.OpenCage
{
    /// <summary>
    /// Street abbreviations in the languages of a country, from <see cref="AddressFormattingData"/>.
    /// "Dr" is "Drive" in Britain but "Dokter" in the Netherlands.
    /// </summary>
    internal sealed class StreetAbbreviations
    {
        private static readonly ConcurrentDictionary<string, StreetAbbreviations> ByCountry = new(StringComparer.OrdinalIgnoreCase);

        private static readonly Regex Token = new(@"[\p{L}\p{N}]+(\.)?", RegexOptions.CultureInvariant);

        // "str" to {"straat"}, and back.
        private readonly Dictionary<string, HashSet<string>> _equivalents = [];

        // For compounds such as "kerkstr" for "kerkstraat".
        // Four letters or more, so "kort" does not match inside names.
        private readonly List<(string Long, string Short)> _suffixes = [];

        // Only abbreviations that stand for one word.
        private readonly Dictionary<string, string> _expansions;

        private StreetAbbreviations(IEnumerable<string> languages)
        {
            var pairs = languages
                .SelectMany(language => AddressFormattingData.RoadAbbreviations.TryGetValue(language, out var entries) ? entries : [])
                .Select(e => (Long: Words.Split(e.Long), Short: Words.Split(e.Short), Written: e.Long))
                // Single words only: "Zone industrielle: Z.I" does not map one word to one word.
                .Where(e => e.Long.Length == 1 && e.Short.Length == 1 && e.Long[0] != e.Short[0])
                .Select(e => (Long: e.Long[0], Short: e.Short[0], e.Written))
                .ToList();

            foreach ((string longForm, string shortForm, _) in pairs)
            {
                Equivalent(longForm, shortForm);
                Equivalent(shortForm, longForm);
                if (longForm.Length >= 4)
                    _suffixes.Add((longForm, shortForm));
            }

            _expansions = pairs
                .GroupBy(p => p.Short)
                .Select(g => (Short: g.Key, Written: g.Select(p => p.Written).Distinct(StringComparer.OrdinalIgnoreCase).ToList()))
                .Where(e => e.Written.Count == 1)
                .ToDictionary(e => e.Short, e => e.Written[0]);
        }

        /// <summary>
        /// All languages when the country is unknown.
        /// </summary>
        public static StreetAbbreviations For(string countryCode)
            => ByCountry.GetOrAdd(countryCode?.Trim() ?? "", code => new StreetAbbreviations(
                AddressFormattingData.CountryLanguages.TryGetValue(code.ToUpperInvariant(), out string[] languages) ? languages : AddressFormattingData.RoadAbbreviations.Keys));

        /// <summary>
        /// Whether two normalized words are equal or abbreviate each other, also in compounds.
        /// </summary>
        public bool Same(string a, string b)
            => a == b
               || (_equivalents.TryGetValue(a, out HashSet<string> equivalents) && equivalents.Contains(b))
               || IsCompoundAbbreviation(a, b)
               || IsCompoundAbbreviation(b, a);

        /// <summary>
        /// Writes out abbreviations, e.g. "Avenue Louise" for "Av. Louise".
        /// One with a vowel only when written with its dot, as "Burg" is also a word.
        /// </summary>
        public string Expand(string text)
            => string.IsNullOrEmpty(text) ? text : Token.Replace(text, match =>
            {
                string[] word = Words.Split(match.Value);
                bool hasDot = match.Groups[1].Success;
                if (word.Length != 1 || (!hasDot && word[0].Any(c => "aeiouy".Contains(c))))
                    return match.Value;

                return _expansions.TryGetValue(word[0], out string written) ? written : match.Value;
            });

        private void Equivalent(string word, string other)
        {
            if (!_equivalents.TryGetValue(word, out HashSet<string> equivalents))
                _equivalents[word] = equivalents = [];
            equivalents.Add(other);
        }

        // "kerkstr" for "kerkstraat".
        private bool IsCompoundAbbreviation(string full, string abbreviated)
            => _suffixes.Any(s => full.Length > s.Long.Length && full.EndsWith(s.Long, StringComparison.Ordinal)
                                  && abbreviated == full[..^s.Long.Length] + s.Short);
    }
}
