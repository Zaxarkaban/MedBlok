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
            LogToFile($"Обязательные врачи: {string.Join(", ", mandatoryDoctors)}");

            var doctorsFromClauses = new List<string>();
            foreach (var clause in selectedClauses)
            {
                if (Dictionaries.OrderClauseDataMap.TryGetValue(clause, out var clauseData))
                {
                    doctorsFromClauses.AddRange(clauseData.Doctors);
                    LogToFile($"Врачи для пункта {clause}: {string.Join(", ", clauseData.Doctors)}");
                }
                else
                {
                    LogToFile($"Пункт {clause} не найден в OrderClauseDataMap.");
                }
            }

            var allDoctors = mandatoryDoctors
                .Concat(doctorsFromClauses)
                .Distinct()
                .ToList();
            LogToFile($"Все врачи после объединения и удаления дубликатов: {string.Join(", ", allDoctors)}");

            if (isFemale)
            {
                if (!allDoctors.Contains("Акушер-гинеколог"))
                {
                    allDoctors.Add("Акушер-гинеколог");
                    LogToFile("Добавлен Акушер-гинеколог (женский пол).");
                }
            }

            allDoctors.Add("Профпатолог");
            LogToFile("Добавлен Профпатолог.");

            LogToFile($"Итоговый список врачей: {string.Join(", ", allDoctors)}");
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
            LogToFile($"Обязательные анализы: {string.Join(", ", mandatoryTests)}");

            if (isOver40)
            {
                mandatoryTests.Add("Измерение внутриглазного давления");
                LogToFile("Добавлен анализ: Измерение внутриглазного давления (возраст > 40).");
            }

            if (isFemale)
            {
                mandatoryTests.Add("Бактериологическое (на флору) и цитологическое (на атипичные клетки) исследования");
                mandatoryTests.Add("Ультразвуковое исследование органов малого таза");
                LogToFile("Добавлены анализы для женщин: Бактериологическое исследование, УЗИ органов малого таза.");
            }

            if (isFemale && isOver40)
            {
                mandatoryTests.Add("Маммография обеих молочных желез в двух проекциях");
                LogToFile("Добавлен анализ: Маммография (женский пол и возраст > 40).");
            }

            var testsFromClauses = new List<string>();
            foreach (var clause in selectedClauses)
            {
                if (Dictionaries.OrderClauseDataMap.TryGetValue(clause, out var clauseData))
                {
                    testsFromClauses.AddRange(clauseData.Tests);
                    LogToFile($"Анализы для пункта {clause}: {string.Join(", ", clauseData.Tests)}");
                }
                else
                {
                    LogToFile($"Пункт {clause} не найден в OrderClauseDataMap.");
                }
            }

            mandatoryTests.AddRange(testsFromClauses.Distinct().Except(mandatoryTests));
            LogToFile($"Итоговый список анализов: {string.Join(", ", mandatoryTests)}");

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

                System.Threading.Thread.Sleep(500);

                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = outputPath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при заполнении PDF: {ex.Message}");
            }
        }

        private void AddTestsPage(PdfDocument pdfDocument, List<string> tests, PdfFont font)
        {
            // Убедимся, что в документе есть как минимум 2 страницы
            int currentPageCount = pdfDocument.GetNumberOfPages();
            while (currentPageCount < 2)
            {
                pdfDocument.AddNewPage();
                currentPageCount++;
            }

            // Получаем вторую страницу
            var page = pdfDocument.GetPage(2);
            var pageSize = page.GetPageSize();

            // Определяем область для левой половины страницы (A4: ширина 595, половина = 297.5)
            var leftHalf = new Rectangle(36, 36, 261.5f, pageSize.GetHeight() - 72); // 36 пунктов отступ слева и снизу, ширина 261.5, высота с учётом отступов

            // Создаём ColumnText для управления позицией текста
            var column = new PdfCanvas(page);
            var columnText = new iText.Layout.Canvas(column, leftHalf);

            // Создаём параграф с текстом
            var paragraph = new Paragraph()
                .SetFont(font)
                .SetFontSize(7);

            // Меняем заголовок на "Список исследований"
            paragraph.Add(new Text("Список исследований:\n\n"));
            int testNumber = 1;
            foreach (var test in tests)
            {
                paragraph.Add(new Text($"{testNumber}. {test}\n"));
                testNumber++;
            }

            // Добавляем параграф на вторую страницу
            columnText.Add(paragraph);
            columnText.Close();
        }

        private void LogToFile(string message)
        {
            try
            {
                string logPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log.txt");
                string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - DocumentService - {message}\n";
                File.AppendAllText(logPath, logEntry);
                Console.WriteLine(logEntry);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при записи в лог: {ex.Message}");
            }
        }
    }
}