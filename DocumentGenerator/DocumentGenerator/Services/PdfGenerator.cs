using iText.Forms;
using iText.Forms.Fields;
using iText.Kernel.Pdf;
using Microsoft.EntityFrameworkCore;
using DocumentGenerator.Data;
using DocumentGenerator.Data.Entities;
using DocumentGenerator.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DocumentGenerator.Services
{
    public class PdfGenerator
    {
        private readonly MainWindowViewModel _viewModel;
        private readonly AppDbContext _context;

        public PdfGenerator(MainWindowViewModel viewModel, AppDbContext context)
        {
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task GeneratePdfAsync(string outputPath, string templatePath)
        {
            try
            {
                // Проверяем, существует ли шаблон
                if (!File.Exists(templatePath))
                {
                    throw new FileNotFoundException($"PDF template not found at: {templatePath}");
                }

                using var reader = new PdfReader(templatePath);
                using var writer = new PdfWriter(outputPath);
                using var pdfDoc = new PdfDocument(reader, writer);
                var form = PdfAcroForm.GetAcroForm(pdfDoc, true);
                var fields = form.GetAllFormFields();

                // Заполняем поля формы
                FillFormFields(fields);

                // Заполняем таблицу врачей
                await FillDoctorsTableAsync(fields);

                form.FlattenFields();
                pdfDoc.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating PDF: {ex.Message}\nStackTrace: {ex.StackTrace}");
                throw; // Оставляем throw, но позже добавим обработку в UI
            }
        }

        private void FillFormFields(IDictionary<string, PdfFormField> fields)
        {
            SetFieldValue(fields, "FullName", _viewModel.FullName);
            SetFieldValue(fields, "Position", _viewModel.Position);
            SetFieldValue(fields, "DateOfBirth", _viewModel.DateOfBirth);
            SetFieldValue(fields, "Gender", _viewModel.Gender);
            SetFieldValue(fields, "Snils", _viewModel.Snils);
            SetFieldValue(fields, "PassportSeries", _viewModel.PassportSeries);
            SetFieldValue(fields, "PassportNumber", _viewModel.PassportNumber);
            SetFieldValue(fields, "PassportIssueDate", _viewModel.PassportIssueDate);
            SetFieldValue(fields, "PassportIssuedBy", _viewModel.PassportIssuedBy);
            SetFieldValue(fields, "Address", _viewModel.Address);
            SetFieldValue(fields, "Phone", _viewModel.Phone);
            SetFieldValue(fields, "MedicalOrganization", _viewModel.MedicalOrganization);
            SetFieldValue(fields, "MedicalPolicy", _viewModel.MedicalPolicy);
            SetFieldValue(fields, "MedicalFacility", _viewModel.MedicalFacility);
            SetFieldValue(fields, "Workplace", _viewModel.Workplace);
            SetFieldValue(fields, "OwnershipForm", _viewModel.OwnershipForm);
            SetFieldValue(fields, "Okved", _viewModel.Okved);
            SetFieldValue(fields, "WorkExperience", _viewModel.WorkExperience);
            SetFieldValue(fields, "Year", DateTime.Now.Year.ToString());
        }

        private async Task FillDoctorsTableAsync(IDictionary<string, PdfFormField> fields)
        {
            var doctors = await GetDoctorsForSelectedClausesAsync();
            var defaultDoctors = new List<string>
            {
                "Терапевт", "Офтальмолог", "Невролог", "Хирург",
                "Отоларинголог", "Дерматовенеролог", "Акушер-гинеколог", "Психиатр"
            };

            // Если врачей меньше, чем ожидается, дополняем списком по умолчанию
            for (int i = 0; i < 8; i++)
            {
                string doctorName = i < doctors.Count ? doctors[i] : (i < defaultDoctors.Count ? defaultDoctors[i] : "Не указано");
                string fieldName = $"Doctor{i + 1}";
                SetFieldValue(fields, fieldName, doctorName);
            }
        }

        private void SetFieldValue(IDictionary<string, PdfFormField> fields, string fieldName, string value)
        {
            if (fields.TryGetValue(fieldName, out var field))
            {
                field.SetValue(value ?? "Не указано");
            }
            else
            {
                Console.WriteLine($"Field {fieldName} not found in the PDF template.");
            }
        }

        private async Task<List<string>> GetDoctorsForSelectedClausesAsync()
        {
            var selectedClauses = _viewModel.SelectedOrderClauses ?? new System.Collections.ObjectModel.ObservableCollection<OrderClause>();
            var clauseTexts = selectedClauses.Select(c => c.ClauseText).ToList();

            if (!clauseTexts.Any())
            {
                return new List<string>(); // Возвращаем пустой список, если нет выбранных пунктов
            }

            return await _context.Doctors
                .Include(d => d.OrderClause)
                .Where(d => d.OrderClause != null && clauseTexts.Contains(d.OrderClause.ClauseText))
                .Select(d => d.DoctorName)
                .Distinct()
                .ToListAsync();
        }
    }
}