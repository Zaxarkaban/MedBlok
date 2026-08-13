using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using iText.Forms;
using iText.Kernel.Font;
using iText.Kernel.Pdf;

namespace DocumentGenerator.Services
{
    /// <summary>
    /// Условные доп. бланки перед страницами врачей:
    /// ПФИ (п.4.1), ВИЧ (п.2.4.2 / 19.1 / 20 / 21), Цитология (женщины).
    /// </summary>
    public static class ConditionalExtraPagesService
    {
        private static readonly string[] HivClauses = { "2.4.2", "19.1", "20", "21" };

        public static void Append(
            PdfDocument pdfDocument,
            IEnumerable<string>? orderClauses,
            bool isFemale,
            IReadOnlyDictionary<string, string> patientData,
            PdfFont font)
        {
            var clauses = NormalizeClauses(orderClauses);

            if (clauses.Contains("4.1"))
                AppendFilledPdf(pdfDocument, "pfi.pdf", patientData, font);

            if (HivClauses.Any(clauses.Contains))
                AppendFilledPdf(pdfDocument, "hiv.pdf", patientData, font);

            if (isFemale)
                AppendFilledPdf(pdfDocument, "cytology.pdf", patientData, font);
        }

        public static Dictionary<string, string> BuildPatientData(
            string? fullName,
            string? dateOfBirth,
            string? gender,
            string? position = null,
            string? workplace = null,
            string? address = null,
            string? phone = null,
            string? passportSeries = null,
            string? passportNumber = null,
            string? workExperience = null,
            string? orderClause = null)
        {
            var parts = (fullName ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries);
            string lastName = parts.Length > 0 ? parts[0] : "";
            string firstName = parts.Length > 1 ? parts[1] : "";
            string middleName = parts.Length > 2 ? string.Join(" ", parts.Skip(2)) : "";

            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["FullName"] = fullName ?? "",
                ["DateOfBirth"] = dateOfBirth ?? "",
                ["Gender"] = GenderHelper.ToCanonical(gender),
                ["Position"] = position ?? "",
                ["Workplace"] = workplace ?? "",
                ["Address"] = address ?? "",
                ["Phone"] = phone ?? "",
                ["PassportSeries"] = passportSeries ?? "",
                ["PassportNumber"] = passportNumber ?? "",
                ["WorkExperience"] = workExperience ?? "",
                ["OrderClause"] = FormatOrderClauses(orderClause),
                ["LastName"] = lastName,
                ["FirstName"] = firstName,
                ["MiddleName"] = middleName,
            };
        }

        /// <summary>Нормализует пункты к виду «п.1.1, п.4.1».</summary>
        public static string FormatOrderClauses(string? orderClauseText)
        {
            if (string.IsNullOrWhiteSpace(orderClauseText))
                return "";

            return FormatOrderClauses(orderClauseText.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries));
        }

        public static string FormatOrderClauses(IEnumerable<string>? clauses)
        {
            if (clauses == null)
                return "";

            return string.Join(", ", clauses
                .Select(c => c.Trim())
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Select(c =>
                {
                    if (c.StartsWith("п.", StringComparison.OrdinalIgnoreCase))
                        return "п." + c.Substring(2).Trim();
                    return "п." + c;
                }));
        }

        private static HashSet<string> NormalizeClauses(IEnumerable<string>? orderClauses)
        {
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (orderClauses == null)
                return result;

            foreach (var raw in orderClauses)
            {
                if (string.IsNullOrWhiteSpace(raw))
                    continue;

                foreach (var part in raw.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    var clause = part.Trim();
                    if (clause.StartsWith("п.", StringComparison.OrdinalIgnoreCase))
                        clause = clause.Substring(2).Trim();
                    if (!string.IsNullOrWhiteSpace(clause))
                        result.Add(clause);
                }
            }

            return result;
        }

        private static void AppendFilledPdf(
            PdfDocument dest,
            string fileName,
            IReadOnlyDictionary<string, string> patientData,
            PdfFont _)
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ExtraForms", fileName);
            if (!File.Exists(path))
            {
                Console.WriteLine($"Доп. бланк не найден: {path}");
                return;
            }

            using var ms = new MemoryStream();
            using (var writer = new PdfWriter(ms))
            {
                writer.SetCloseStream(false);
                using var reader = new PdfReader(path);
                using var doc = new PdfDocument(reader, writer);
                // Шрифт из dest нельзя использовать в другом PdfDocument
                var localFont = PdfFontHelper.CreateTimesFont();
                var form = PdfAcroForm.GetAcroForm(doc, true);
                if (form != null)
                {
                    var fields = form.GetAllFormFields();
                    foreach (var data in patientData)
                        PdfFormFieldHelper.SetFieldValue(fields, data.Key, data.Value, localFont);
                    form.FlattenFields();
                }
            }

            ms.Position = 0;
            using var filledReader = new PdfReader(ms);
            using var filled = new PdfDocument(filledReader);
            filled.CopyPagesTo(1, filled.GetNumberOfPages(), dest);
        }
    }
}
