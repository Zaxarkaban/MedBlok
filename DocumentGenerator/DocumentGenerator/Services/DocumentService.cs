using System;
using System.Collections.Generic;
using System.Linq;
using iText.Kernel.Pdf;
using iText.Forms;
using DocumentGenerator.Models;
using iText.Kernel.Font;
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
                        PdfFormFieldHelper.SetFieldValue(fields, data.Key, data.Value, font);
                    }

                    for (int i = 0; i < doctors.Count && i < 11; i++)
                    {
                        string fieldName = $"Doctor_{i + 1}";
                        if (fields.ContainsKey(fieldName))
                        {
                            PdfFormFieldHelper.SetFieldValue(fields, fieldName, doctors[i], font);
                        }
                        else
                        {
                            Console.WriteLine($"Поле {fieldName} не найдено в шаблоне.");
                        }
                    }

                    if (doctors.Count > 11)
                    {
                        Console.WriteLine($"Внимание: В списке {doctors.Count} врачей, но шаблон поддерживает только 11. Лишние врачи проигнорированы.");
                    }

                    int age = 0;
                    bool isFemale = userData.TryGetValue("Gender", out var gender) && GenderHelper.IsFemale(gender);
                    if (userData.TryGetValue("DateOfBirth", out var dob) && DateTime.TryParseExact(dob, "dd.MM.yyyy", null, System.Globalization.DateTimeStyles.None, out var birthDate))
                    {
                        var today = DateTime.Today;
                        age = today.Year - birthDate.Year;
                        if (birthDate.Date > today.AddYears(-age)) age--;
                    }
                    bool isOver40 = age > 40;

                    var tests = GenerateTestsList(isOver40, isFemale, userData.TryGetValue("OrderClause", out var clauses) ? clauses.Split(", ").ToList() : new List<string>());
                    TestsListPdfService.DrawOnFirstPage(pdfDocument, tests, font);

                    userData.TryGetValue("FullName", out var fullName);
                    userData.TryGetValue("DateOfBirth", out var dateOfBirth);
                    userData.TryGetValue("Position", out var position);
                    userData.TryGetValue("Workplace", out var workplace);
                    userData.TryGetValue("Address", out var address);
                    userData.TryGetValue("Phone", out var phone);
                    userData.TryGetValue("PassportSeries", out var passportSeries);
                    userData.TryGetValue("PassportNumber", out var passportNumber);

                    userData.TryGetValue("WorkExperience", out var workExperience);

                    var selectedClauses = userData.TryGetValue("OrderClause", out var orderClauseText)
                        ? orderClauseText.Split(new[] { ", ", "," }, StringSplitOptions.RemoveEmptyEntries).ToList()
                        : new List<string>();

                    var extraPatientData = ConditionalExtraPagesService.BuildPatientData(
                        fullName,
                        dateOfBirth,
                        gender,
                        position,
                        workplace,
                        address,
                        phone,
                        passportSeries,
                        passportNumber,
                        workExperience,
                        ConditionalExtraPagesService.FormatOrderClauses(selectedClauses));
                    ConditionalExtraPagesService.Append(
                        pdfDocument,
                        selectedClauses,
                        isFemale,
                        extraPatientData,
                        font);

                    DoctorExamPagesService.AppendDoctorExamPages(pdfDocument, doctors, extraPatientData);

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
    }
}