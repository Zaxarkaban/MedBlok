using System;

namespace DocumentGenerator.Services
{
    /// <summary>
    /// Нормализация пола из Excel/UI: Ж, ж, Жен, Женский, М, мужской и т.п.
    /// </summary>
    public static class GenderHelper
    {
        public static bool IsFemale(string? gender)
        {
            var t = NormalizeToken(gender);
            if (t.Length == 0)
                return false;

            return t is "ж" or "жен" or "женск" or "женский" or "female" or "f" or "w"
                   || t.StartsWith("жен", StringComparison.Ordinal);
        }

        public static bool IsMale(string? gender)
        {
            var t = NormalizeToken(gender);
            if (t.Length == 0)
                return false;

            return t is "м" or "муж" or "мужск" or "мужской" or "male" or "m"
                   || t.StartsWith("муж", StringComparison.Ordinal);
        }

        /// <summary>Каноническое значение для полей PDF: «Женский» / «Мужской» / исходная строка.</summary>
        public static string ToCanonical(string? gender)
        {
            if (IsFemale(gender))
                return "Женский";
            if (IsMale(gender))
                return "Мужской";
            return string.IsNullOrWhiteSpace(gender) ? "" : gender.Trim();
        }

        private static string NormalizeToken(string? gender)
        {
            if (string.IsNullOrWhiteSpace(gender))
                return "";

            return gender.Trim()
                .Replace('\u00A0', ' ')
                .ToLowerInvariant()
                .Replace('ё', 'е');
        }
    }
}
