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

        // Normalized word to the words it stands for or is short for, e.g. "str" to {"straat"} and "straat" to {"str"}.
        private readonly Dictionary<string, HashSet<string>> _equivalents = [];

        // Pairs whose full word may end a compound, e.g. ("straat", "str") for "kerkstraat" and "kerkstr".
        // Four letters or more, so a short word such as "kort" does not match inside names.
        private readonly List<(string Long, string Short)> _suffixes = [];

        // Normalized abbreviation to the full word as written, only where it stands for one word.
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
        /// The abbreviations for a country (ISO 3166-1 alpha-2), or of all languages when the country is unknown.
        /// </summary>
        public static StreetAbbreviations For(string countryCode)
            => ByCountry.GetOrAdd(countryCode?.Trim() ?? "", code => new StreetAbbreviations(
                AddressFormattingData.CountryLanguages.TryGetValue(code.ToUpperInvariant(), out string[] languages) ? languages : AddressFormattingData.RoadAbbreviations.Keys));

        /// <summary>
        /// Whether two normalized words name the same: equal, a word and its abbreviation ("straat", "str"), or a compound and its abbreviation ("kerkstraat", "kerkstr").
        /// </summary>
        public bool Same(string a, string b)
            => a == b
               || (_equivalents.TryGetValue(a, out HashSet<string> equivalents) && equivalents.Contains(b))
               || IsCompoundAbbreviation(a, b)
               || IsCompoundAbbreviation(b, a);

        /// <summary>
        /// The text with abbreviations written out where they stand for one word, e.g. "Avenue Louise 149" for "Av. Louise 149".
        /// An abbreviation with a vowel ("Av", "Burg") is only written out with its dot, as without it it may be an ordinary word ("Burg" is a castle in German).
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

        // "kerkstr" for "kerkstraat": the same stem with the full word at the end abbreviated.
        private bool IsCompoundAbbreviation(string full, string abbreviated)
            => _suffixes.Any(s => full.Length > s.Long.Length && full.EndsWith(s.Long, StringComparison.Ordinal)
                                  && abbreviated == full[..^s.Long.Length] + s.Short);
    }
}
