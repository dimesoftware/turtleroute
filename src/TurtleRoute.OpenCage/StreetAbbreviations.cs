using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace TurtleRoute.OpenCage
{
    /// <summary>
    /// Street-name abbreviations from OpenCage's address-formatting data (see AddressFormatting/README.md), limited to the languages of a country.
    /// "Dr" is "Drive" in Britain and "Dokter" in the Netherlands, so the country decides which pairs apply.
    /// </summary>
    internal sealed class StreetAbbreviations
    {
        private static readonly Lazy<(Dictionary<string, List<(string Long, string Short)>> ByLanguage, Dictionary<string, string[]> LanguagesByCountry)> Data = new(Load);

        private static readonly Regex Token = new(@"[\p{L}\p{N}]+(\.)?", RegexOptions.CultureInvariant);

        // Normalized single-word pairs, e.g. ("straat", "str"); the display form of the long word for writing out queries.
        private readonly List<(string Long, string Short, string Display)> _pairs;

        private StreetAbbreviations(List<(string Long, string Short, string Display)> pairs) => _pairs = pairs;

        /// <summary>
        /// The abbreviations for a country (ISO 3166-1 alpha-2), or for all languages when the country is unknown.
        /// </summary>
        public static StreetAbbreviations For(string countryCode)
        {
            var (byLanguage, languagesByCountry) = Data.Value;
            IEnumerable<string> languages = !string.IsNullOrWhiteSpace(countryCode) && languagesByCountry.TryGetValue(countryCode.Trim().ToUpperInvariant(), out string[] known)
                ? known
                : byLanguage.Keys;

            List<(string, string, string)> pairs = [];
            foreach (string language in languages)
            {
                if (!byLanguage.TryGetValue(language, out var entries))
                    continue;

                foreach ((string longForm, string shortForm) in entries)
                {
                    string[] longWords = Words.Split(longForm), shortWords = Words.Split(shortForm);
                    // Single words only: "Zone industrielle: Z.I" does not map one word to one word.
                    if (longWords.Length == 1 && shortWords.Length == 1 && longWords[0] != shortWords[0])
                        pairs.Add((longWords[0], shortWords[0], longForm));
                }
            }

            return new StreetAbbreviations(pairs);
        }

        /// <summary>
        /// Whether two normalized words name the same thing: equal, a word and its abbreviation ("straat", "str"),
        /// or a compound and its abbreviated form ("kerkstraat", "kerkstr").
        /// </summary>
        public bool Same(string a, string b)
        {
            if (a == b)
                return true;

            foreach ((string longForm, string shortForm, _) in _pairs)
            {
                if (IsAbbreviation(a, b, longForm, shortForm) || IsAbbreviation(b, a, longForm, shortForm))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// The query with abbreviations written out where they are unambiguous for the country, e.g. "Avenue Louise 149" for "Av. Louise 149".
        /// Abbreviations with a vowel ("Av", "Burg") are only written out with their dot, as without it they may be ordinary words ("Burg" is a castle in German);
        /// without vowels ("Bd", "Stwg", "Rd") they cannot be.
        /// </summary>
        public string Expand(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            return Token.Replace(text, match =>
            {
                string[] word = Words.Split(match.Value);
                if (word.Length != 1)
                    return match.Value;

                bool hasDot = match.Groups[1].Success;
                if (!hasDot && word[0].Any(IsVowel))
                    return match.Value;

                List<string> longForms = [.. _pairs.Where(p => p.Short == word[0]).Select(p => p.Display).Distinct(StringComparer.OrdinalIgnoreCase)];
                return longForms.Count == 1 ? longForms[0] : match.Value;
            });
        }

        // "a" is "b" with its ending abbreviated: "str" for "straat", or "kerkstr" for "kerkstraat".
        private static bool IsAbbreviation(string full, string abbreviated, string longForm, string shortForm)
        {
            if (full == longForm)
                return abbreviated == shortForm;

            // Compounds only for real words, so "dr" does not turn "...dokter" into "...dr" in the middle of names.
            return longForm.Length >= 4
                && full.Length > longForm.Length && full.EndsWith(longForm, StringComparison.Ordinal)
                && abbreviated.Length > shortForm.Length && abbreviated.EndsWith(shortForm, StringComparison.Ordinal)
                && full.AsSpan(0, full.Length - longForm.Length).SequenceEqual(abbreviated.AsSpan(0, abbreviated.Length - shortForm.Length));
        }

        private static bool IsVowel(char c) => "aeiouy".Contains(c);

        private static (Dictionary<string, List<(string, string)>>, Dictionary<string, string[]>) Load()
        {
            Assembly assembly = typeof(StreetAbbreviations).Assembly;
            Dictionary<string, List<(string, string)>> byLanguage = [];
            Dictionary<string, string[]> languagesByCountry = [];

            foreach (string name in assembly.GetManifestResourceNames().Where(n => n.StartsWith("AddressFormatting/", StringComparison.Ordinal)))
            {
                string[] lines = Read(assembly, name);
                string file = Path.GetFileNameWithoutExtension(name.Replace('\\', '/'));

                if (file == "country2lang")
                {
                    foreach ((string key, string value) in Entries(lines))
                        languagesByCountry[key.ToUpperInvariant()] = [.. value.Split(',').Select(l => l.Trim()).Where(l => l.Length > 0)];
                }
                else if (file == "supplement")
                {
                    foreach ((string section, string longForm, string shortForm) in Sections(lines))
                        Add(byLanguage, section, longForm, shortForm);
                }
                else
                {
                    // An abbreviations file per language, of which only the road section applies.
                    foreach ((string section, string longForm, string shortForm) in Sections(lines).Where(e => e.Section == "road"))
                        Add(byLanguage, file, longForm, shortForm);
                }
            }

            return (byLanguage, languagesByCountry);
        }

        private static void Add(Dictionary<string, List<(string, string)>> byLanguage, string language, string longForm, string shortForm)
        {
            if (!byLanguage.TryGetValue(language, out var entries))
                byLanguage[language] = entries = [];
            entries.Add((longForm, shortForm));
        }

        private static string[] Read(Assembly assembly, string name)
        {
            using Stream stream = assembly.GetManifestResourceStream(name);
            using StreamReader reader = new(stream);
            return reader.ReadToEnd().Split('\n');
        }

        // "key: value" lines at the top level.
        private static IEnumerable<(string Key, string Value)> Entries(string[] lines)
            => lines.Where(l => !l.TrimStart().StartsWith('#') && l.Contains(':'))
                .Select(l => (Key: l[..l.IndexOf(':')].Trim(), Value: l[(l.IndexOf(':') + 1)..].Trim()))
                .Where(e => e.Key.Length > 0 && e.Value.Length > 0);

        // "section:" lines followed by indented "Long: Short" lines.
        private static IEnumerable<(string Section, string Long, string Short)> Sections(string[] lines)
        {
            string section = null;
            foreach (string line in lines)
            {
                if (line.Trim().Length == 0 || line.TrimStart().StartsWith('#'))
                    continue;

                int colon = line.IndexOf(':');
                if (colon < 0)
                    continue;

                if (!char.IsWhiteSpace(line[0]))
                {
                    section = line[..colon].Trim();
                    continue;
                }

                string longForm = line[..colon].Trim(), shortForm = line[(colon + 1)..].Trim().Trim('"', '\'');
                if (section != null && longForm.Length > 0 && shortForm.Length > 0)
                    yield return (section, longForm, shortForm);
            }
        }
    }
}
