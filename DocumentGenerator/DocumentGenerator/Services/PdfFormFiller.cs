using iText.Forms;
using iText.Forms.Fields;
using iText.IO.Font;
using iText.Kernel.Font;
using iText.Kernel.Pdf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DocumentGenerator.Services
{
    public interface IPdfFormFiller
    {
        void FillToFile(string templatePath, string outputPath, IReadOnlyDictionary<string, object?> values);
    }

    public sealed class PdfFormFiller : IPdfFormFiller
    {
        public void FillToFile(string templatePath, string outputPath, IReadOnlyDictionary<string, object?> values)
        {
            if (string.IsNullOrWhiteSpace(templatePath) || !File.Exists(templatePath))
                throw new FileNotFoundException("PDF template not found.", templatePath);

            using var reader = new PdfReader(templatePath);
            using var writer = new PdfWriter(outputPath);
            using var pdfDocument = new PdfDocument(reader, writer);

            var form = PdfAcroForm.GetAcroForm(pdfDocument, true);
            var fields = form.GetAllFormFields();

            string fontPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Fonts", "times.ttf");
            if (!File.Exists(fontPath))
                throw new FileNotFoundException("Times New Roman font file not found.", fontPath);

            PdfFontFactory.Register(fontPath);
            var font = PdfFontFactory.CreateFont(fontPath, PdfEncodings.IDENTITY_H, PdfFontFactory.EmbeddingStrategy.FORCE_EMBEDDED);

            foreach (var kv in values)
            {
                var fieldName = kv.Key;
                if (string.IsNullOrWhiteSpace(fieldName))
                    continue;

                if (!fields.TryGetValue(fieldName, out var pdfField))
                    continue;

                ApplyValue(pdfField, kv.Value, font);
            }

            form.FlattenFields();
            pdfDocument.Close();
        }

        private static void ApplyValue(PdfFormField field, object? value, PdfFont font)
        {
            switch (value)
            {
                case null:
                    field.SetValue("");
                    break;

                case bool b:
                    // Common checkbox on/off value in your templates is "V"
                    field.SetValue(b ? "V" : "");
                    break;

                case IEnumerable<string> list:
                    field.SetValue(string.Join(", ", list));
                    break;

                default:
                    field.SetValue(value.ToString() ?? "");
                    break;
            }

            field.SetFontAndSize(font, 10);
            field.RegenerateField();
        }
    }
}

