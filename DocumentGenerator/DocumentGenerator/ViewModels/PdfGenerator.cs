using iText.Forms;
using iText.Forms.Fields;
using iText.Kernel.Pdf;
using iText.Kernel.Font;
using iText.IO.Font;
using System;
using System.IO;
using DocumentGenerator.ViewModels;
using System.Linq;
using System.Collections.Generic;
using iText.Kernel.Pdf.Canvas;
using iText.Kernel.Geom;
using iText.Layout.Element;

namespace DocumentGenerator
{
    public class PdfGenerator
    {
        private readonly MainWindowViewModel _viewModel;

        public PdfGenerator(MainWindowViewModel viewModel)
        {
            _viewModel = viewModel;
        }

        public void GeneratePdf(string outputPath, string templatePath)
        {
            if (string.IsNullOrEmpty(templatePath) || !File.Exists(templatePath))
            {
                throw new FileNotFoundException("PDF template not found at the specified path.", templatePath);
            }

            try
            {
                using (var writer = new PdfWriter(outputPath))
                using (var pdf = new PdfDocument(new PdfReader(templatePath), writer))
                {
                    var form = PdfAcroForm.GetAcroForm(pdf, true);
                    var fields = form.GetAllFormFields();

                    // Загружаем шрифт Times New Roman из проекта
                    string fontPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Fonts", "times.ttf");
                    if (!File.Exists(fontPath))
                    {
                        throw new FileNotFoundException("Times New Roman font file not found in the project.", fontPath);
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

                    // Вычисляем возраст и пол
                    int age = 0;
                    bool isFemale = _viewModel.Gender == "Женский";
                    if (!string.IsNullOrEmpty(_viewModel.DateOfBirth) && DateTime.TryParseExact(_viewModel.DateOfBirth, "dd.MM.yyyy", null, System.Globalization.DateTimeStyles.None, out var dob))
                    {
                        var today = DateTime.Today;
                        age = today.Year - dob.Year;
                        if (dob.Date > today.AddYears(-age)) age--;
                    }
                    bool isOver40 = age > 40;

                    // Заполняем поля в шаблоне
                    SetFieldValue(fields, "FullName", _viewModel.FullName, font);
                    SetFieldValue(fields, "Position", _viewModel.Position, font);
                    SetFieldValue(fields, "DateOfBirth", _viewModel.DateOfBirth, font);
                    SetFieldValue(fields, "Gender", _viewModel.Gender, font);
                    SetFieldValue(fields, "Snils", _viewModel.Snils, font);
                    SetFieldValue(fields, "PassportSeries", _viewModel.PassportSeries, font);
                    SetFieldValue(fields, "PassportNumber", _viewModel.PassportNumber, font);
                    SetFieldValue(fields, "PassportIssueDate", _viewModel.PassportIssueDate, font);
                    SetFieldValue(fields, "PassportIssuedBy", _viewModel.PassportIssuedBy, font);
                    SetFieldValue(fields, "Address", _viewModel.Address, font);
                    SetFieldValue(fields, "Phone", _viewModel.Phone, font);
                    SetFieldValue(fields, "MedicalOrganization", _viewModel.MedicalOrganization, font);
                    SetFieldValue(fields, "MedicalPolicy", _viewModel.MedicalPolicy, font);
                    SetFieldValue(fields, "MedicalFacility", _viewModel.MedicalFacility, font);
                    SetFieldValue(fields, "Workplace", _viewModel.Workplace, font);
                    SetFieldValue(fields, "OwnershipForm", _viewModel.OwnershipForm, font);
                    SetFieldValue(fields, "Okved", _viewModel.Okved, font);
                    SetFieldValue(fields, "WorkExperience", _viewModel.WorkExperience, font);
                    SetFieldValue(fields, "OrderClause", string.Join(", ", _viewModel.SelectedOrderClauses), font);

                    // Создаём список врачей с учётом условий
                    var mandatoryDoctors = new List<string> { "Терапевт", "Невролог", "Психиатр", "Нарколог" };
                    var doctorsFromClauses = _viewModel.GetDoctorsForSelectedClauses();
                    var allDoctors = mandatoryDoctors
                        .Concat(doctorsFromClauses)
                        .Distinct()
                        .ToList();

                    if (isOver40)
                    {
                        if (!allDoctors.Contains("Офтальмолог"))
                            allDoctors.Add("Офтальмолог");
                    }

                    if (isFemale)
                    {
                        if (!allDoctors.Contains("Акушер-гинеколог"))
                            allDoctors.Add("Акушер-гинеколог");
                    }

                    if (isFemale && isOver40)
                    {
                        if (!allDoctors.Contains("Радиолог"))
                            allDoctors.Add("Радиолог");
                    }

                    allDoctors.Add("Профпатолог");

                    // Заполняем врачей в поля Doctor_1 до Doctor_12
                    for (int i = 0; i < allDoctors.Count && i < 12; i++)
                    {
                        string fieldName = $"Doctor_{i + 1}";
                        SetFieldValue(fields, fieldName, allDoctors[i], font);
                    }

                    if (allDoctors.Count > 12)
                    {
                        Console.WriteLine($"Внимание: В списке {allDoctors.Count} врачей, но шаблон поддерживает только 12. Лишние врачи проигнорированы.");
                    }

                    // Генерируем список анализов
                    var tests = GenerateTestsList(isOver40, isFemale);

                    // Добавляем новый лист с анализами на третью страницу
                    AddTestsPage(pdf, tests, font);

                    // Сохраняем изменения
                    form.FlattenFields();
                    pdf.Close();
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Ошибка при создании PDF: {ex.Message}", ex);
            }
        }

        private void SetFieldValue(IDictionary<string, PdfFormField> fields, string fieldName, string value, PdfFont font)
        {
            if (fields.ContainsKey(fieldName))
            {
                var field = fields[fieldName];
                field.SetValue(value ?? "");
                field.SetFontAndSize(font, 10);
            }
        }

        private List<string> GenerateTestsList(bool isOver40, bool isFemale)
        {
            var mandatoryTests = new List<string>
            {
                "Расчет на основании антропометрии (измерение роста, массы тела, окружности талии) индекса массы тела",
                "Электрокардиография в покое",
                "Измерение артериального давления на периферических артериях",
                "Флюорография или рентгенография легких в двух проекциях (прямая и правая боковая)",
                "Определение относительного сердечно-сосудистого риска",
                "Общий анализ крови (гемоглобин, цветной показатель, эритроциты, тромбоциты, лейкоциты, лейкоцитарная формула, СОЭ)",
                "Клинический анализ мочи (удельный вес, белок, сахар, микроскопия осадка)",
                "Определение уровня общего холестерина в крови (допускается использование экспресс-метода)",
                "Исследование уровня глюкозы в крови натощак (допускается использование экспресс-метода)"
            };

            if (isOver40)
            {
                mandatoryTests.Add("Определение абсолютного сердечно-сосудистого риска");
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

            return mandatoryTests;
        }

        private void AddTestsPage(PdfDocument pdfDocument, List<string> tests, PdfFont font)
        {
            // Убедимся, что в документе есть как минимум 3 страницы
            int currentPageCount = pdfDocument.GetNumberOfPages();
            while (currentPageCount < 3)
            {
                pdfDocument.AddNewPage();
                currentPageCount++;
            }

            // Получаем третью страницу
            var page = pdfDocument.GetPage(3);
            var pageSize = page.GetPageSize();
            var canvas = new PdfCanvas(page);

            // Создаём ColumnText для управления позицией текста
            var column = new iText.Kernel.Pdf.Canvas.PdfCanvas(page);
            var columnText = new iText.Layout.Canvas(column, new Rectangle(36, 36, pageSize.GetWidth() - 72, pageSize.GetHeight() - 72));

            // Создаём параграф с текстом
            var paragraph = new Paragraph()
                .SetFont(font)
                .SetFontSize(12);

            paragraph.Add(new Text("Список необходимых анализов:\n\n"));
            int testNumber = 1;
            foreach (var test in tests)
            {
                paragraph.Add(new Text($"{testNumber}. {test}\n"));
                testNumber++;
            }

            // Добавляем параграф на третью страницу
            columnText.Add(paragraph);
            columnText.Close();
        }
    }
}