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
                    string fontPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Fonts", "times.ttf");
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

                    // Создаём список врачей (аналогично DocumentService.GenerateDoctorsList)
                    var mandatoryDoctors = new List<string> { "Терапевт", "Невролог", "Психиатр", "Нарколог" };
                    var doctorsFromClauses = _viewModel.GetDoctorsForSelectedClauses();
                    var allDoctors = mandatoryDoctors
                        .Concat(doctorsFromClauses)
                        .Distinct()
                        .ToList();
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
    }
}