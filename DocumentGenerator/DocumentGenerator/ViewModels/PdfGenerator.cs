using iText.Forms;
using iText.Forms.Fields;
using iText.Kernel.Pdf;
using iText.Kernel.Font;
using iText.IO.Font;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DocumentGenerator.ViewModels;
using DocumentGenerator.Data;
using iText.IO.Font.Constants;

namespace DocumentGenerator
{
    public class PdfGenerator
    {
        private readonly MainWindowViewModel _viewModel;
        private readonly string _dbPath;

        public PdfGenerator(MainWindowViewModel viewModel, DatabaseInitializer dbInitializer)
        {
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            _dbPath = dbInitializer.GetDbPath();
        }

        public void GeneratePdf(string outputPath, string templatePath)
        {
            if (string.IsNullOrEmpty(templatePath) || !File.Exists(templatePath))
            {
                throw new FileNotFoundException("PDF template not found at the specified path.", templatePath);
            }

            try
            {
                using (var reader = new PdfReader(templatePath))
                using (var writer = new PdfWriter(outputPath))
                using (var pdf = new PdfDocument(reader, writer))
                {
                    var form = PdfAcroForm.GetAcroForm(pdf, true);
                    var fields = form.GetAllFormFields(); // Исправлено с GetFormFields на GetAllFormFields

                    // Используем встроенный шрифт Times-Roman с UTF-8 для поддержки кириллицы
                    string fontPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Fonts", "times.ttf");
                    if (!File.Exists(fontPath))
                    {
                        throw new FileNotFoundException("Times New Roman font file not found in the project.", fontPath);
                    }
                    PdfFont font = PdfFontFactory.CreateFont(fontPath, PdfEncodings.IDENTITY_H, PdfFontFactory.EmbeddingStrategy.FORCE_EMBEDDED);

                    // Заполняем поля шаблона
                    SetFieldValue(form, fields, "FullName", _viewModel.FullName ?? "");
                    SetFieldValue(form, fields, "Position", _viewModel.Position ?? "");
                    SetFieldValue(form, fields, "DateOfBirth", _viewModel.DateOfBirth ?? "");
                    SetFieldValue(form, fields, "Gender", _viewModel.Gender ?? "");
                    SetFieldValue(form, fields, "Snils", _viewModel.Snils ?? "");
                    SetFieldValue(form, fields, "PassportSeries", _viewModel.PassportSeries ?? "");
                    SetFieldValue(form, fields, "PassportNumber", _viewModel.PassportNumber ?? "");
                    SetFieldValue(form, fields, "PassportIssueDate", _viewModel.PassportIssueDate ?? "");
                    SetFieldValue(form, fields, "PassportIssuedBy", _viewModel.PassportIssuedBy ?? "");
                    SetFieldValue(form, fields, "Address", _viewModel.Address ?? "");
                    SetFieldValue(form, fields, "Phone", _viewModel.Phone ?? "");
                    SetFieldValue(form, fields, "MedicalOrganization", _viewModel.MedicalOrganization ?? "");
                    SetFieldValue(form, fields, "MedicalPolicy", _viewModel.MedicalPolicy ?? "");
                    SetFieldValue(form, fields, "MedicalFacility", _viewModel.MedicalFacility ?? "");
                    SetFieldValue(form, fields, "Workplace", _viewModel.Workplace ?? "");
                    SetFieldValue(form, fields, "OwnershipForm", _viewModel.OwnershipForm ?? "");
                    SetFieldValue(form, fields, "Okved", _viewModel.Okved ?? "");
                    SetFieldValue(form, fields, "WorkExperience", _viewModel.WorkExperience ?? "");

                    // Получаем и заполняем врачей
                    var doctors = GetDoctorsForSelectedClauses();
                    for (int i = 0; i < doctors.Count; i++)
                    {
                        string fieldName = $"Doctor{i + 1}";
                        SetFieldValue(form, fields, fieldName, doctors[i] ?? "");
                    }

                    // Сохраняем изменения
                    form.FlattenFields();
                    pdf.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating PDF: {ex.Message}\nStackTrace: {ex.StackTrace}");
                throw;
            }
        }

        private void SetFieldValue(PdfAcroForm form, IDictionary<string, PdfFormField> fields, string fieldName, string value)
        {
            if (fields.ContainsKey(fieldName))
            {
                var field = fields[fieldName];
                field.SetValue(value);
                field.SetFont(PdfFontFactory.CreateFont(StandardFonts.TIMES_ROMAN, PdfEncodings.UTF8, PdfFontFactory.EmbeddingStrategy.PREFER_EMBEDDED));
                field.SetFontSize(10);
            }
            else
            {
                Console.WriteLine($"Field {fieldName} not found in template.");
            }
        }

        private List<string> GetDoctorsForSelectedClauses()
        {
            var doctors = new List<string>();

            if (string.IsNullOrEmpty(_dbPath))
            {
                throw new InvalidOperationException("Database path is not initialized.");
            }

            using (var connection = new SqliteConnection($"Data Source={_dbPath}"))
            {
                connection.Open();

                foreach (var clause in _viewModel.SelectedOrderClauses ?? new System.Collections.ObjectModel.ObservableCollection<OrderClause>())
                {
                    var command = connection.CreateCommand();
                    command.CommandText = @"
                        SELECT d.DoctorName
                        FROM Doctors d
                        JOIN OrderClauses c ON d.ClauseId = c.Id
                        WHERE c.ClauseText = $clauseText";
                    command.Parameters.AddWithValue("$clauseText", clause.ClauseText ?? "");

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            doctors.Add(reader.GetString(0));
                        }
                    }
                }
            }

            return doctors.Distinct().ToList();
        }
    }
}