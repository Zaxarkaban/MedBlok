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
    public class NewFormPdfGenerator
    {
        public void GeneratePdf(Dictionary<string, string> userData, string outputPath, string templatePath = "карта водительская комиссия.pdf")
        {
            try
            {
                using (var pdfReader = new PdfReader(templatePath)) using (var pdfWriter = new PdfWriter(outputPath)) using (var pdfDocument = new PdfDocument(pdfReader, pdfWriter))
                {
                    var form = PdfAcroForm.GetAcroForm(pdfDocument, true); var fields = form.GetAllFormFields();

                    if (!fields.Any())
                    {
                        throw new InvalidOperationException("PDF-шаблон не содержит полей для заполнения.");
                    }

                    PdfFont font;
                    try
                    {
                        font = PdfFontHelper.CreateTimesFont();
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException($"Ошибка при загрузке шрифта: {ex.Message}", ex);
                    }

                    foreach (var data in userData)
                    {
                        if (fields.TryGetValue(data.Key, out var field))
                        {
                            field.SetValue(data.Value);
                            field.SetFontAndSize(font, 10);
                            field.RegenerateField();
                        }
                        else
                        {
                            Console.WriteLine($"Поле {data.Key} не найдено в шаблоне.");
                        }
                    }

                    form.FlattenFields();
                    pdfDocument.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при заполнении PDF: {ex.Message}");
                throw;
            }
        }
    }

}