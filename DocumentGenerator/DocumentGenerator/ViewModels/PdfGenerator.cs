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

namespace DocumentGenerator
{
    public class PdfGenerator
    {
        private readonly MainWindowViewModel _viewModel;
        private readonly string _dbPath;

        public PdfGenerator(MainWindowViewModel viewModel)
        {
            _viewModel = viewModel;
            _dbPath = new DatabaseInitializer().GetDbPath();
        }

        public void GeneratePdf(string outputPath, string templatePath)
        {
            if (string.IsNullOrEmpty(templatePath) || !File.Exists(templatePath))
            {
                throw new FileNotFoundException("PDF template not found at the specified path.", templatePath);
            }

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
                PdfFont font = PdfFontFactory.CreateFont(fontPath, PdfEncodings.IDENTITY_H, PdfFontFactory.EmbeddingStrategy.FORCE_EMBEDDED);

                // Заполняем поля в шаблоне
                if (fields.ContainsKey("FullName"))
                {
                    var field = fields["FullName"];
                    field.SetValue(_viewModel.FullName ?? "");
                    field.SetFontAndSize(font, 10);
                }
                if (fields.ContainsKey("Position"))
                {
                    var field = fields["Position"];
                    field.SetValue(_viewModel.Position ?? "");
                    field.SetFontAndSize(font, 10);
                }
                if (fields.ContainsKey("DateOfBirth"))
                {
                    var field = fields["DateOfBirth"];
                    field.SetValue(_viewModel.DateOfBirth ?? "");
                    field.SetFontAndSize(font, 10);
                }
                if (fields.ContainsKey("Gender"))
                {
                    var field = fields["Gender"];
                    field.SetValue(_viewModel.Gender ?? "");
                    field.SetFontAndSize(font, 10);
                }
                if (fields.ContainsKey("Snils"))
                {
                    var field = fields["Snils"];
                    field.SetValue(_viewModel.Snils ?? "");
                    field.SetFontAndSize(font, 10);
                }
                if (fields.ContainsKey("PassportSeries"))
                {
                    var field = fields["PassportSeries"];
                    field.SetValue(_viewModel.PassportSeries ?? "");
                    field.SetFontAndSize(font, 10);
                }
                if (fields.ContainsKey("PassportNumber"))
                {
                    var field = fields["PassportNumber"];
                    field.SetValue(_viewModel.PassportNumber ?? "");
                    field.SetFontAndSize(font, 10);
                }
                if (fields.ContainsKey("PassportIssueDate"))
                {
                    var field = fields["PassportIssueDate"];
                    field.SetValue(_viewModel.PassportIssueDate ?? "");
                    field.SetFontAndSize(font, 10);
                }
                if (fields.ContainsKey("PassportIssuedBy"))
                {
                    var field = fields["PassportIssuedBy"];
                    field.SetValue(_viewModel.PassportIssuedBy ?? "");
                    field.SetFontAndSize(font, 10);
                }
                if (fields.ContainsKey("Address"))
                {
                    var field = fields["Address"];
                    field.SetValue(_viewModel.Address ?? "");
                    field.SetFontAndSize(font, 10);
                }
                if (fields.ContainsKey("Phone"))
                {
                    var field = fields["Phone"];
                    field.SetValue(_viewModel.Phone ?? "");
                    field.SetFontAndSize(font, 10);
                }
                if (fields.ContainsKey("MedicalOrganization"))
                {
                    var field = fields["MedicalOrganization"];
                    field.SetValue(_viewModel.MedicalOrganization ?? "");
                    field.SetFontAndSize(font, 10);
                }
                if (fields.ContainsKey("MedicalPolicy"))
                {
                    var field = fields["MedicalPolicy"];
                    field.SetValue(_viewModel.MedicalPolicy ?? "");
                    field.SetFontAndSize(font, 10);
                }
                if (fields.ContainsKey("MedicalFacility"))
                {
                    var field = fields["MedicalFacility"];
                    field.SetValue(_viewModel.MedicalFacility ?? "");
                    field.SetFontAndSize(font, 10);
                }
                if (fields.ContainsKey("Workplace"))
                {
                    var field = fields["Workplace"];
                    field.SetValue(_viewModel.Workplace ?? "");
                    field.SetFontAndSize(font, 10);
                }
                if (fields.ContainsKey("OwnershipForm"))
                {
                    var field = fields["OwnershipForm"];
                    field.SetValue(_viewModel.OwnershipForm ?? "");
                    field.SetFontAndSize(font, 10);
                }
                if (fields.ContainsKey("Okved"))
                {
                    var field = fields["Okved"];
                    field.SetValue(_viewModel.Okved ?? "");
                    field.SetFontAndSize(font, 10);
                }
                if (fields.ContainsKey("WorkExperience"))
                {
                    var field = fields["WorkExperience"];
                    field.SetValue(_viewModel.WorkExperience ?? "");
                    field.SetFontAndSize(font, 10);
                }

                // Получаем список врачей для выбранных пунктов
                var doctors = GetDoctorsForSelectedClauses();
                // Заполняем поля врачей в PDF (Doctor1, Doctor2, и т.д.)
                for (int i = 0; i < doctors.Count; i++)
                {
                    string fieldName = $"Doctor{i + 1}";
                    if (fields.ContainsKey(fieldName))
                    {
                        var field = fields[fieldName];
                        field.SetValue(doctors[i]);
                        field.SetFontAndSize(font, 10);
                    }
                }

                // Сохраняем изменения
                form.FlattenFields();
                pdf.Close();
            }
        }

        private List<string> GetDoctorsForSelectedClauses()
        {
            var doctors = new List<string>();

            using (var connection = new SqliteConnection($"Data Source={_dbPath}"))
            {
                connection.Open();

                // Получаем врачей для каждого выбранного пункта
                foreach (var clause in _viewModel.SelectedOrderClauses)
                {
                    var command = connection.CreateCommand();
                    command.CommandText = @"
                        SELECT d.DoctorName
                        FROM Doctors d
                        JOIN OrderClauses c ON d.ClauseId = c.Id
                        WHERE c.ClauseText = $clause";
                    command.Parameters.AddWithValue("$clause", clause);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            doctors.Add(reader.GetString(0));
                        }
                    }
                }
            }

            // Удаляем дубликаты врачей
            return doctors.Distinct().ToList();
        }
    }
}