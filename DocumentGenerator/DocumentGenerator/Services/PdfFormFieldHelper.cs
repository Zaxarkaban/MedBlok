using iText.Forms.Fields;
using iText.Kernel.Font;
using System.Collections.Generic;
using System.Linq;

namespace DocumentGenerator.Services
{
    public static class PdfFormFieldHelper
    {
        private const float DefaultInitialFontSize = 9f;
        private const float MinFontSize = 4f;
        private const float ShrinkStep = 0.25f;
        private const float CurrentYearFontSize = 24f;

        public static string GetYearsWord(int count)
        {
            var n = System.Math.Abs(count) % 100;
            if (n is >= 11 and <= 14)
                return "лет";

            return (n % 10) switch
            {
                1 => "год",
                2 or 3 or 4 => "года",
                _ => "лет"
            };
        }

        public static string FormatWorkExperienceYears(string? years)
        {
            if (!int.TryParse(years?.Trim(), out int count))
                count = 0;

            return $"{count} {GetYearsWord(count)}";
        }

        public static void SetFieldValue(
            IDictionary<string, PdfFormField> fields,
            string fieldName,
            string? value,
            PdfFont font,
            float initialFontSize = DefaultInitialFontSize)
        {
            if (!fields.TryGetValue(fieldName, out var pdfField))
                return;

            var text = value ?? "";
            pdfField.SetValue(text);

            if (fieldName == "CurrentYear")
            {
                pdfField.SetFontAndSize(font, CurrentYearFontSize);
                return;
            }

            float fontSize = initialFontSize;
            pdfField.SetFontAndSize(font, fontSize);

            var widget = pdfField.GetWidgets().FirstOrDefault();
            if (widget == null)
                return;

            var rect = widget.GetRectangle();
            float fieldWidth = rect.GetAsNumber(2).FloatValue() - rect.GetAsNumber(0).FloatValue();

            while (!string.IsNullOrEmpty(text) && fontSize > MinFontSize)
            {
                float textWidth = font.GetWidth(text, fontSize);
                if (textWidth <= fieldWidth)
                    break;

                fontSize -= ShrinkStep;
            }

            pdfField.SetFontAndSize(font, fontSize);
        }
    }
}
