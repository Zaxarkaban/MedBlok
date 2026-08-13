using System;

namespace DocumentGenerator.Services
{
    /// <summary>Нормализация и поиск пунктов приказа / вредности.</summary>
    public static class OrderClauseSearch
    {
        public static string Normalize(string? query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return "";

            var t = query.Trim()
                .Replace('\u00A0', ' ')
                .ToLowerInvariant()
                .Replace('ё', 'е');

            if (t.StartsWith("п.", StringComparison.Ordinal))
                t = t[2..].TrimStart();
            else if (t.StartsWith("п ", StringComparison.Ordinal))
                t = t[2..].TrimStart();

            return t;
        }

        public static bool Matches(string clause, string normalizedQuery)
        {
            if (normalizedQuery.Length == 0)
                return true;

            var c = clause.Trim()
                .Replace('\u00A0', ' ')
                .ToLowerInvariant()
                .Replace('ё', 'е');

            return c.Contains(normalizedQuery, StringComparison.Ordinal)
                   || Normalize(clause).Contains(normalizedQuery, StringComparison.Ordinal);
        }
    }
}
