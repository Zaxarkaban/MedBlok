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
using DocumentGenerator.Models;
using iText.Layout;
using iText.Layout.Renderer;
using iText.Layout.Layout;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace DocumentGenerator
{
    public class PdfGenerator
    {
        private readonly MainWindowViewModel _viewModel;

        public PdfGenerator(MainWindowViewModel viewModel)
        {
            _viewModel = viewModel;
        }

        // Метод для получения списка исследований с прямым соответствием из таблицы
        private List<string> GetTestsWithDirectMatch()
        {
            return new List<string>
            {
                "Исследование крови на сифилис",
                "Исследование уровня аспартат-трансаминазы и аланин-трансаминазы",
                "Исследование уровня креатинина",
                "Исследование уровня мочевины",
                "Исследование уровня калия",
                "Исследование уровня натрия",
                "Исследование уровня железа",
                "Исследование уровня щелочной фосфатазы",
                "Исследование уровня билирубина",
                "Исследование уровня общего белка",
                "Исследование уровня триглицеридов",
                "Исследование уровня холестерина",
                "Исследование уровня фибриногена",
                "Исследование уровня ретикулоцитов в крови",
                "Исследование уровня метгемоглобина в крови",
                "Исследование уровня карбоксигемоглобина в крови",
                "Исследование уровня ретикулоцитов, метгемоглобина в крови",
                "Исследование уровня ретикулоцитов, тромбоцитов в крови",
                "Определение группы крови и резус-фактора"
            };
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
                    SetFieldValue(fields, "DateOfBirth1", _viewModel.DateOfBirth, font);
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
                    SetFieldValue(fields, "WorkExperience", $"{_viewModel.WorkExperienceYears} лет {_viewModel.WorkExperienceMonths} месяцев", font);
                    SetFieldValue(fields, "OrderClause", string.Join(", ", _viewModel.SelectedOrderClauses), font);
                    SetFieldValue(fields, "WorkAddress", _viewModel.WorkAddress, font);
                    SetFieldValue(fields, "Department", _viewModel.Department, font);
                    SetFieldValue(fields, "ServicePoint", _viewModel.ServicePoint ?? "", font);
                    fields["обязательные_анализы"].SetValue("V"); // Устанавливаем галочку для обязательных анализов
                    

                    // Текущий год
                    int currentYear = DateTime.Now.Year;
                    SetFieldValue(fields, "CurrentYear", currentYear.ToString(), font);
                    SetFieldValue(fields, "CurrentYear1", currentYear.ToString(), font);

                    // Новая дата (полная)
                    string currentDate = DateTime.Now.ToString("dd.MM.yyyy"); // Формат: 22.04.2025
                    SetFieldValue(fields, "CurrentDate", currentDate, font);

                    //Вот тут бахнуть вычисление возраста
                    SetFieldValue(fields, "normasDate", age.ToString(), font); // Заполняем поле возраста в годах
                    // Разбиваем ФИО на части
                    string fio = _viewModel.FullName ?? "";
                    string[] fioParts = fio.Split(' ');
                    string lastName = fioParts.Length > 0 ? fioParts[0] : "";
                    string firstName = fioParts.Length > 1 ? fioParts[1] : "";
                    string middleName = fioParts.Length > 2 ? fioParts[2] : "";
                    SetFieldValue(fields, "LastName", lastName, font);
                    SetFieldValue(fields, "FirstName", firstName, font);
                    SetFieldValue(fields, "MiddleName", middleName, font);

                    // Устанавливаем галочку в зависимости от выбранного ServicePoint
                    switch (_viewModel.ServicePoint)
                    {
                        case "ПО 67":
                            if (fields.ContainsKey("ServicePoint1"))
                            {
                                fields["ServicePoint1"].SetValue("V");
                            }
                            break;
                        case "ПО 89":
                            if (fields.ContainsKey("ServicePoint2"))
                            {
                                fields["ServicePoint2"].SetValue("V");
                            }
                            break;
                        case "ПО 66":
                            if (fields.ContainsKey("ServicePoint3"))
                            {
                                fields["ServicePoint3"].SetValue("V");
                            }
                            break;
                        case "ПО «Шушары»":
                            if (fields.ContainsKey("ServicePoint4"))
                            {
                                fields["ServicePoint4"].SetValue("V");
                            }
                            break;
                        case "ЖК «Шушары»":
                            if (fields.ContainsKey("ServicePoint5"))
                            {
                                fields["ServicePoint5"].SetValue("V");
                            }
                            break;
                        case "ПО «Славянка»":
                            if (fields.ContainsKey("ServicePoint6"))
                            {
                                fields["ServicePoint6"].SetValue("V");
                            }
                            break;
                    }

                    // Поле "Документ"
                    SetFieldValue(fields, "Document", "паспорт", font);

                    // Создаём список врачей с учётом условий
                    var mandatoryDoctors = new List<string> { "Терапевт", "Невролог", "Психиатр", "Нарколог" };
                    var doctorsFromClauses = _viewModel.GetDoctorsForSelectedClauses();
                    var allDoctors = mandatoryDoctors
                        .Concat(doctorsFromClauses)
                        .Distinct()
                        .ToList();

                    if (isFemale)
                    {
                        if (!allDoctors.Contains("Акушер-гинеколог"))
                            allDoctors.Add("Акушер-гинеколог");
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

                    // Генерируем список исследований
                    var tests = GenerateTestsList(isOver40, isFemale);

                    // Получаем список исследований с прямым соответствием
                    var testsWithDirectMatch = GetTestsWithDirectMatch();

                    // Сравниваем и устанавливаем галочки
                    foreach (var test in tests)
                    {
                        if (testsWithDirectMatch.Contains(test))
                        {
                            string fieldName = $"test_{SanitizeFieldName(test)}";
                            if (fields.ContainsKey(fieldName))
                            {
                                fields[fieldName].SetValue("V"); // Устанавливаем галочку
                            }
                        }
                    }

                    // Добавляем новый лист с исследованиями на третью страницу
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

        private string SanitizeFieldName(string name)
        {
            // Удаляем недопустимые символы для имени поля в PDF
            return name.Replace(" ", "_")
                       .Replace("(", "")
                       .Replace(")", "")
                       .Replace(",", "")
                       .Replace(".", "")
                       .Replace(":", "")
                       .Replace(";", "")
                       .Replace("/", "_");
        }

        private void SetFieldValue(IDictionary<string, PdfFormField> fields, string fieldName, string value, PdfFont font)
        {
            if (fields.TryGetValue(fieldName, out var pdfField))
            {
                // Устанавливаем значение поля
                pdfField.SetValue(value);

                // Пропускаем динамическое изменение шрифта для CurrentYear (шрифт фиксированно 24)
                if (fieldName == "CurrentYear")
                {
                    pdfField.SetFontAndSize(font, 24f);
                    return;
                }

                // Начальный размер шрифта
                float fontSize = 10.5f;
                pdfField.SetFontAndSize(font, fontSize);

                // Получаем размеры поля
                var widget = pdfField.GetWidgets().FirstOrDefault();
                if (widget == null) return;
                var rect = widget.GetRectangle();
                float fieldWidth = rect.GetAsNumber(2).FloatValue() - rect.GetAsNumber(0).FloatValue(); // Ширина поля

                // Проверяем, влезает ли текст, уменьшаем шрифт при необходимости
                float textWidth;
                float minFontSize = 6f; // Минимальный размер шрифта
                do
                {
                    // Измеряем ширину текста с текущим размером шрифта
                    textWidth = font.GetWidth(value, fontSize);

                    if (textWidth > fieldWidth && fontSize > minFontSize)
                    {
                        fontSize -= 0.5f; // Уменьшаем шрифт на 0.5
                    }
                    else
                    {
                        break; // Текст влезает или достигнут минимальный шрифт
                    }
                } while (true);

                // Устанавливаем финальный размер шрифта
                pdfField.SetFontAndSize(font, fontSize);
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

            // Добавляем исследования из выбранных пунктов
            var testsFromClauses = new List<string>();
            foreach (var clause in _viewModel.SelectedOrderClauses)
            {
                if (Dictionaries.OrderClauseDataMap.TryGetValue(clause, out var clauseData))
                {
                    testsFromClauses.AddRange(clauseData.Tests);
                }
            }

            // Добавляем уникальные исследования из пунктов в конец списка
            mandatoryTests.AddRange(testsFromClauses.Distinct().Except(mandatoryTests));

            return mandatoryTests;
        }

        private void AddTestsPage(PdfDocument pdfDocument, List<string> tests, PdfFont font)
        {
            // Убедимся, что в документе есть как минимум 11 страниц
            int currentPageCount = pdfDocument.GetNumberOfPages();
            while (currentPageCount < 11)
            {
                pdfDocument.AddNewPage();
                currentPageCount++;
            }

            // Получаем 11-ю страницу
            var page = pdfDocument.GetPage(11);
            var pageSize = page.GetPageSize();

            // Определяем область для всей страницы с отступами (A4: ширина 595, высота 842)
            var fullPage = new Rectangle(36, 36, pageSize.GetWidth() - 72, pageSize.GetHeight() - 72); // Отступы 36 пунктов со всех сторон

            // Создаём PdfCanvas и iText.Layout.Canvas для управления позицией текста
            var column = new PdfCanvas(page);
            var columnText = new iText.Layout.Canvas(column, fullPage);

            // Создаём параграф с текстом
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

            // Добавляем параграф на 11-ю страницу
            columnText.Add(paragraph);
            columnText.Close();
        }
    }
}