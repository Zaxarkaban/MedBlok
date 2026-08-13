using iText.Forms;
using iText.Forms.Fields;
using iText.Kernel.Pdf;
using iText.Kernel.Font;
using System;
using System.IO;
using DocumentGenerator.ViewModels;
using System.Linq;
using System.Collections.Generic;
using DocumentGenerator.Models;
using DocumentGenerator.Services;

namespace DocumentGenerator
{
    public class PdfGenerator
    {
        private readonly MainWindowViewModel _viewModel;

        public PdfGenerator(MainWindowViewModel viewModel)
        {
            _viewModel = viewModel;
        }

        // Метод для получения списка исследований с прямым соответствием из таблицы (В этом году не используется из-за перехода на новые бланки)
        //private List<string> GetTestsWithDirectMatch()
        //{
        //    return new List<string>
        //    {
        //        "Исследование крови на сифилис",
        //        "Исследование уровня аспартат-трансаминазы и аланин-трансаминазы",
        //        "Исследование уровня креатинина",
        //        "Исследование уровня мочевины",
        //        "Исследование уровня калия",
        //        "Исследование уровня натрия",
        //        "Исследование уровня железа",
        //        "Исследование уровня щелочной фосфатазы",
        //        "Исследование уровня билирубина",
        //        "Исследование уровня общего белка",
        //        "Исследование уровня триглицеридов",
        //        "Исследование уровня холестерина",
        //        "Исследование уровня фибриногена",
        //        "Исследование уровня ретикулоцитов в крови",
        //        "Исследование уровня метгемоглобина в крови",
        //        "Исследование уровня карбоксигемоглобина в крови",
        //        "Исследование уровня ретикулоцитов, метгемоглобина в крови",
        //        "Исследование уровня ретикулоцитов, тромбоцитов в крови",
        //        "Определение группы крови и резус-фактора"
        //    };
        //}

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

                    PdfFont font;
                    try
                    {
                        font = PdfFontHelper.CreateTimesFont();
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException($"Ошибка при загрузке шрифта: {ex.Message}", ex);
                    }

                    // Вычисляем возраст и пол
                    int age = 0;
                    bool isFemale = GenderHelper.IsFemale(_viewModel.Gender);
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
                    SetFieldValue(fields, "Address1", _viewModel.Address, font);
                    SetFieldValue(fields, "Phone", _viewModel.Phone, font);
                    SetFieldValue(fields, "MedicalOrganization", _viewModel.MedicalOrganization, font);
                    SetFieldValue(fields, "MedicalPolicy", _viewModel.MedicalPolicy, font);
                    SetFieldValue(fields, "MedicalFacility", _viewModel.MedicalFacility, font);
                    SetFieldValue(fields, "Workplace", _viewModel.Workplace, font);
                    SetFieldValue(fields, "OwnershipForm", _viewModel.OwnershipForm, font);
                    SetFieldValue(fields, "Okved", _viewModel.Okved, font);
                    SetFieldValue(fields, "WorkExperience", PdfFormFieldHelper.FormatWorkExperienceYears(_viewModel.WorkExperienceYears), font);
                    var formattedClauses = _viewModel.SelectedOrderClauses.Select(clause => $"п.{clause}");
                    SetFieldValue(fields, "OrderClause", string.Join(", ", formattedClauses), font);
                    SetFieldValue(fields, "WorkAddress", _viewModel.WorkAddress, font);
                    SetFieldValue(fields, "Department", _viewModel.Department, font);
                    SetFieldValue(fields, "ServicePoint", _viewModel.ServicePoint ?? "", font);
                    //fields["обязательные_анализы"].SetValue("V"); // Устанавливаем галочку для обязательных анализов
                    

                    // Текущий год
                    int currentYear = DateTime.Now.Year;
                    SetFieldValue(fields, "CurrentYear", currentYear.ToString(), font);
                    SetFieldValue(fields, "CurrentYear1", currentYear.ToString(), font);

                    // Новая дата (полная)
                    string currentDate = DateTime.Now.ToString("dd.MM.yyyy"); // Формат: 22.04.2025
                    SetFieldValue(fields, "CurrentDate", currentDate, font);

                    //Вот тут бахнуть вычисление возраста
                    SetFieldValue(fields, "Age", age.ToString(), font);
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

                    // Заполняем врачей в поля Doctor_1 до Doctor_11
                    for (int i = 0; i < allDoctors.Count && i < 11; i++)
                    {
                        string fieldName = $"Doctor_{i + 1}";
                        SetFieldValue(fields, fieldName, allDoctors[i], font);
                    }

                    if (allDoctors.Count > 11)
                    {
                        Console.WriteLine($"Внимание: В списке {allDoctors.Count} врачей, но шаблон поддерживает только 11. Лишние врачи проигнорированы.");
                    }

                    // Генерируем список исследований
                    var tests = GenerateTestsList(isOver40, isFemale);

                    TestsListPdfService.DrawOnFirstPage(pdf, tests, font);

                    var extraPatientData = ConditionalExtraPagesService.BuildPatientData(
                        _viewModel.FullName,
                        _viewModel.DateOfBirth,
                        _viewModel.Gender,
                        _viewModel.Position,
                        _viewModel.Workplace,
                        _viewModel.Address,
                        _viewModel.Phone,
                        _viewModel.PassportSeries,
                        _viewModel.PassportNumber,
                        PdfFormFieldHelper.FormatWorkExperienceYears(_viewModel.WorkExperienceYears),
                        ConditionalExtraPagesService.FormatOrderClauses(_viewModel.SelectedOrderClauses));
                    ConditionalExtraPagesService.Append(
                        pdf,
                        _viewModel.SelectedOrderClauses,
                        isFemale,
                        extraPatientData,
                        font);

                    DoctorExamPagesService.AppendDoctorExamPages(pdf, allDoctors, extraPatientData);

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

        private static void SetFieldValue(IDictionary<string, PdfFormField> fields, string fieldName, string? value, PdfFont font)
        {
            PdfFormFieldHelper.SetFieldValue(fields, fieldName, value, font);
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
    }
}