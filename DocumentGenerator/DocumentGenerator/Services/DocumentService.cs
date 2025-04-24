using System;
using System.Collections.Generic;
using System.Linq;
using iText.Kernel.Pdf;
using iText.Forms;
using DocumentGenerator.Models;
using iText.Kernel.Pdf.Canvas;
using iText.Layout;
using iText.Layout.Element;
using iText.Kernel.Font;
using iText.IO.Font;
using iText.Kernel.Geom;
using System.IO;

namespace DocumentGenerator.Services
{
    public class DocumentService
    {
        public List<string> GenerateDoctorsList(List<string> selectedClauses, bool isOver40, bool isFemale)
        {
            var mandatoryDoctors = new List<string> { "Терапевт", "Невролог", "Психиатр", "Нарколог" };

            var doctorsFromClauses = new List<string>();
            foreach (var clause in selectedClauses)
            {
                if (Dictionaries.OrderClauseDataMap.TryGetValue(clause, out var clauseData))
                {
                    doctorsFromClauses.AddRange(clauseData.Doctors);
                }
            }

            var allDoctors = mandatoryDoctors
                .Concat(doctorsFromClauses)
                .Distinct()
                .ToList();

            if (isFemale)
            {
                if (!allDoctors.Contains("Акушер-гинеколог"))
                {
                    allDoctors.Add("Акушер-гинеколог");
                }
            }

            allDoctors.Add("Профпатолог");

            return allDoctors;
        }

        public List<string> GenerateTestsList(bool isOver40, bool isFemale, List<string> selectedClauses)
        {
            var mandatoryTests = new List<string>
            {
                "Расчет на основании антропометрии (измерение роста, массы тела, окружности талии) индекса массы тела",
                "Электрокардиография в покое",
                "Измерение артериального давления на периферических артериях",
                "Флюорография или рентгенография легких в двух проекциях (прямая и правая боковая)",
                isOver40 ? "Определение абсолютного сердечно-сосудистого риска" : "Определение относительного сердечно-сосудистого риска",
                "Общий анализ крови (гемоглобин, цветной показатель, эритроциты, тромбоциты, лейкоциты, лейкоцитарная формула, СОЭ)",
                "Клинический анализ мочи (удельный вес, белок, сахар, микроскопия осадка)",
                "Определение уровня общего холестерина в крови (допускается использование экспресс-метода)",
                "Исследование уровня глюкозы в крови натощак (допускается использование экспресс-метода)"
            };

            if (isOver40)
            {
                mandatoryTests.Add("Измерение внутриглазного давления");
            }

            if (isFemale)
            {
                mandatoryTests.Add("Бактериологическое (на флору) и цитологическое (на атипичные клетки) исследования");
                mandatoryTests.Add("Ультразвуковое исследование органов малого таза");
            }

            if (isFemale && isOver40)
            {
                mandatoryTests.Add("Маммография обеих молочных желез в двух проекциях");
            }

            var testsFromClauses = new List<string>();
            foreach (var clause in selectedClauses)
            {
                if (Dictionaries.OrderClauseDataMap.TryGetValue(clause, out var clauseData))
                {
                    testsFromClauses.AddRange(clauseData.Tests);
                }
            }

            mandatoryTests.AddRange(testsFromClauses.Distinct().Except(mandatoryTests));

            return mandatoryTests;
        }

        public void FillPdfTemplate(Dictionary<string, string> userData, List<string> doctors, string templatePath = "template.pdf")
        {
            string outputPath = $"output_{DateTime.Now:yyyyMMddHHmmss}.pdf";

            try
            {
                using (var pdfReader = new PdfReader(templatePath))
                using (var pdfWriter = new PdfWriter(outputPath))
                using (var pdfDocument = new PdfDocument(pdfReader, pdfWriter))
                {
                    var form = PdfAcroForm.GetAcroForm(pdfDocument, true);
                    var fields = form.GetAllFormFields();

                    string fontPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Fonts", "times.ttf");
                    if (!File.Exists(fontPath))
                    {
                        throw new FileNotFoundException("Times New Roman font file not found.", fontPath);
                    }

                    PdfFont font;
                    try
                    {
                        PdfFontFactory.Register(fontPath);
                        font = PdfFontFactory.CreateFont(fontPath, PdfEncodings.IDENTITY_H, PdfFontFactory.EmbeddingStrategy.FORCE_EMBEDDED);
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
                    }

                    for (int i = 0; i < doctors.Count && i < 12; i++)
                    {
                        string fieldName = $"Doctor_{i + 1}";
                        if (fields.TryGetValue(fieldName, out var doctorField))
                        {
                            doctorField.SetValue(doctors[i]);
                            doctorField.SetFontAndSize(font, 10);
                            doctorField.RegenerateField();
                        }
                        else
                        {
                            Console.WriteLine($"Поле {fieldName} не найдено в шаблоне.");
                        }
                    }

                    if (doctors.Count > 12)
                    {
                        Console.WriteLine($"Внимание: В списке {doctors.Count} врачей, но шаблон поддерживает только 12. Лишние врачи проигнорированы.");
                    }

                    int age = 0;
                    bool isFemale = userData.TryGetValue("Gender", out var gender) && gender == "Женский";
                    if (userData.TryGetValue("DateOfBirth", out var dob) && DateTime.TryParseExact(dob, "dd.MM.yyyy", null, System.Globalization.DateTimeStyles.None, out var birthDate))
                    {
                        var today = DateTime.Today;
                        age = today.Year - birthDate.Year;
                        if (birthDate.Date > today.AddYears(-age)) age--;
                    }
                    bool isOver40 = age > 40;

                    var tests = GenerateTestsList(isOver40, isFemale, userData.TryGetValue("OrderClause", out var clauses) ? clauses.Split(", ").ToList() : new List<string>());
                    AddTestsPage(pdfDocument, tests, font);

                    form.FlattenFields();
                    pdfDocument.Close();
                }

                Console.WriteLine($"PDF generated at: {outputPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при заполнении PDF: {ex.Message}");
            }
        }

        private void AddTestsPage(PdfDocument pdfDocument, List<string> tests, PdfFont font)
        {
            int currentPageCount = pdfDocument.GetNumberOfPages();
            while (currentPageCount < 2)
            {
                pdfDocument.AddNewPage();
                currentPageCount++;
            }

            var page = pdfDocument.GetPage(2);
            var pageSize = page.GetPageSize();

            var leftHalf = new Rectangle(36, 36, 261.5f, pageSize.GetHeight() - 72);

            var column = new PdfCanvas(page);
            var columnText = new iText.Layout.Canvas(column, leftHalf);

            var paragraph = new Paragraph()
                .SetFont(font)
                .SetFontSize(7);

            paragraph.Add(new Text("Список исследований:\n\n"));
            int testNumber = 1;
            foreach (var test in tests)
            {
                paragraph.Add(new Text($"{testNumber}. {test}\n"));
                testNumber++;
            }

            columnText.Add(paragraph);
            columnText.Close();
        }
    }
}