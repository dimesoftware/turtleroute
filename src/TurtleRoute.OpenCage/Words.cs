using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace TurtleRoute.OpenCage
{
    internal static class Words
    {
        /// <summary>
        /// Lower case words without accents or punctuation, e.g. "São Paulo - SP" becomes ["sao", "paulo", "sp"], and "ß" as "ss".
        /// </summary>
        public static string[] Split(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return [];

            StringBuilder builder = new(value.Length);
            foreach (char c in Decompose(value.ToLowerInvariant().Replace("ß", "ss")))
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    builder.Append(char.IsLetterOrDigit(c) ? c : ' ');
            }

            return builder.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        }

        // Abbreviations of street types, compared as the full word.
        // "st" is left out: it means "street" in English but "saint" in French and in "St. Paul's".
        private static readonly Dictionary<string, string> Abbreviations = new()
        {
            ["av"] = "avenue", ["ave"] = "avenue", ["bd"] = "boulevard", ["bld"] = "boulevard", ["blvd"] = "boulevard", ["chee"] = "chaussee",
            ["rd"] = "road", ["dr"] = "drive", ["ln"] = "lane", ["sq"] = "square", ["rte"] = "route", ["imp"] = "impasse", ["stwg"] = "steenweg",
            ["str"] = "strasse", ["straat"] = "strasse"
        };

        // Street-type suffixes that are written onto the name, shortened alike so "Kerkstr" matches "Kerkstraat" and "Hauptstr" matches "Hauptstrasse".
        private static readonly (string Suffix, string Short)[] Suffixes = [("strasse", "str"), ("straat", "str"), ("steenweg", "stwg")];

        // Street types that come before the name, with the articles that may follow them, e.g. "Avenue des" in "Avenue des Champs-Elysees".
        private static readonly HashSet<string> StreetTypes =
            ["rue", "avenue", "boulevard", "chaussee", "place", "chemin", "allee", "impasse", "route", "quai", "square", "cours", "voie", "dreve",
             "via", "viale", "piazza", "corso", "strada", "calle", "avenida", "plaza", "paseo", "carrer", "rua", "travessa", "largo"];

        private static readonly HashSet<string> Articles =
            ["de", "du", "des", "la", "le", "les", "l", "d", "del", "della", "delle", "dei", "di", "da", "do", "dos", "das", "el", "los", "las"];

        // Unambiguous street-type abbreviations OpenCage does not understand, written out before searching: "Av. Louise" finds only the postcode, "Avenue Louise" the building.
        private static readonly Regex QueryAbbreviation = new(@"(?<![\p{L}\p{N}])(av|ave|bd|blvd|ch[eé]e|stwg|rd)\.?(?![\p{L}\p{N}])", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        private static readonly Dictionary<string, string> QueryExpansions = new(StringComparer.OrdinalIgnoreCase)
        {
            ["av"] = "Avenue", ["ave"] = "Avenue", ["bd"] = "Boulevard", ["blvd"] = "Boulevard", ["chee"] = "Chaussée", ["chée"] = "Chaussée", ["stwg"] = "Steenweg", ["rd"] = "Road"
        };

        /// <summary>
        /// The query with street-type abbreviations written out, e.g. "Avenue Louise 149" for "Av. Louise 149".
        /// </summary>
        public static string ExpandAbbreviations(string value)
            => string.IsNullOrEmpty(value) ? value : QueryAbbreviation.Replace(value, match => QueryExpansions[match.Groups[1].Value]);

        /// <summary>
        /// The word as compared in street names: abbreviations written out and street-type suffixes shortened, e.g. "kerkstraat" and "kerkstr" both as "kerkstr".
        /// One word in, one word out, so positions in the query stay the same.
        /// </summary>
        public static string Canonical(string word)
        {
            string full = Abbreviations.TryGetValue(word, out string expanded) ? expanded : word;
            foreach ((string suffix, string shortForm) in Suffixes)
            {
                if (full.EndsWith(suffix, StringComparison.Ordinal))
                    return full[..^suffix.Length] + shortForm;
            }

            return full;
        }

        public static string[] Canonical(string[] words) => [.. words.Select(Canonical)];

        /// <summary>
        /// Where <paramref name="street"/> (canonical words) is named in <paramref name="words"/> (canonical words), and how many words it spans, or -1.
        /// The query may leave out the leading street type and articles, e.g. "Champs-Elysees" for "Avenue des Champs-Elysees", but not name another one:
        /// "Avenue du Chateau" and "Rue du Chateau" are different streets.
        /// </summary>
        public static int IndexOfStreet(string[] words, string[] street, out int length)
        {
            length = street.Length;
            int start = IndexOf(words, street);
            if (start >= 0)
                return start;

            int typeEnd = 0;
            while (typeEnd < street.Length && StreetTypes.Contains(street[typeEnd]))
                typeEnd++;
            int nameStart = typeEnd;
            while (nameStart < street.Length && Articles.Contains(street[nameStart]))
                nameStart++;

            // The rest must still name the street, not just "de" or a letter.
            string[] name = street[nameStart..];
            if (typeEnd == 0 || name.Sum(word => word.Length) < 4)
                return -1;

            start = IndexOf(words, name);
            if (start < 0)
                return -1;

            int before = start - 1;
            while (before >= 0 && Articles.Contains(words[before]))
                before--;
            if (before >= 0 && StreetTypes.Contains(words[before]) && !street[..typeEnd].Contains(words[before]))
                return -1;

            length = name.Length;
            return start;
        }

        /// <summary>
        /// German umlauts as written without them, e.g. "Fuerth" for "Fürth".
        /// </summary>
        public static string ExpandUmlauts(string value)
            => value.Replace("ä", "ae").Replace("ö", "oe").Replace("ü", "ue").Replace("Ä", "Ae").Replace("Ö", "Oe").Replace("Ü", "Ue");

        /// <summary>
        /// The position of <paramref name="part"/> as consecutive words in <paramref name="words"/>, or -1.
        /// </summary>
        public static int IndexOf(string[] words, string[] part)
        {
            for (int start = 0; part.Length > 0 && start + part.Length <= words.Length; start++)
            {
                if (part.SequenceEqual(words.Skip(start).Take(part.Length)))
                    return start;
            }

            return -1;
        }

        public static bool StartsWith(string[] words, string[] part)
            => part.Length > 0 && part.Length <= words.Length && part.SequenceEqual(words.Take(part.Length));

        // Normalize throws on invalid text, such as a lone surrogate.
        private static string Decompose(string value)
        {
            try
            {
                return value.Normalize(NormalizationForm.FormD);
            }
            catch (ArgumentException)
            {
                return value;
            }
        }
    }
}
