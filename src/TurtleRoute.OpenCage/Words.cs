using System;
using System.Globalization;
using System.Linq;
using System.Text;

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
