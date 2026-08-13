using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DocumentGenerator.Services
{
    public static class ValidationSummaryHelper
    {
        public static string? BuildMessage(params (string Label, string? Error)[] fields)
        {
            var errors = fields
                .Where(f => !string.IsNullOrWhiteSpace(f.Error))
                .ToList();

            if (errors.Count == 0)
                return null;

            var sb = new StringBuilder();
            sb.AppendLine("Заполните обязательные поля и исправьте ошибки:");
            sb.AppendLine();
            foreach (var (label, error) in errors)
                sb.AppendLine($"• {label}: {error}");

            return sb.ToString().TrimEnd();
        }

        public static string? BuildMessage(IEnumerable<(string Label, string? Error)> fields)
            => BuildMessage(fields.ToArray());
    }
}
