using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using OfficeOpenXml;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using DocumentGenerator.Data;
using DocumentGenerator.Data.Entities;
using DocumentGenerator.ViewModels;
using DocumentGenerator.Models;
using System.Linq;

namespace DocumentGenerator.ViewModels
{
    public class ExcelDataViewModel : ViewModelBase
    {
        private readonly MainWindowViewModel _mainViewModel;
        private readonly AppDbContext _context; // Добавляем контекст базы данных
        private ObservableCollection<Record> _records = new ObservableCollection<Record>();

        public MainWindowViewModel MainViewModel => _mainViewModel;

        public ObservableCollection<Record> Records
        {
            get => _records;
            set => SetProperty(ref _records, value);
        }

        public ExcelDataViewModel(MainWindowViewModel mainViewModel, AppDbContext context)
        {
            _mainViewModel = mainViewModel ?? throw new ArgumentNullException(nameof(mainViewModel));
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public ExcelDataViewModel()
        {
            if (!Design.IsDesignMode)
            {
                throw new InvalidOperationException("This constructor is only for design-time use. Use the parameterized constructor with DI.");
            }
            _mainViewModel = new MainWindowViewModel();
            _records = new ObservableCollection<Record>();
        }

        public async Task<bool> LoadFromExcelAsync(IStorageFile file)
        {
            if (file == null)
            {
                Console.WriteLine("Файл не выбран.");
                return false;
            }

            try
            {
                using var stream = await file.OpenReadAsync();
                using var package = new ExcelPackage(stream);
                var worksheet = package.Workbook.Worksheets.FirstOrDefault();

                if (worksheet == null)
                {
                    throw new InvalidOperationException("Лист Excel не найден.");
                }

                _mainViewModel.FullName = worksheet.Cells[1, 1].Text?.Trim() ?? string.Empty;
                _mainViewModel.Position = worksheet.Cells[1, 2].Text?.Trim() ?? string.Empty;
                _mainViewModel.DateOfBirth = worksheet.Cells[1, 3].Text?.Trim() ?? string.Empty;
                _mainViewModel.Gender = worksheet.Cells[1, 4].Text?.Trim() ?? string.Empty;
                _mainViewModel.Snils = worksheet.Cells[1, 5].Text?.Trim() ?? string.Empty;
                _mainViewModel.PassportSeries = worksheet.Cells[1, 6].Text?.Trim() ?? string.Empty;
                _mainViewModel.PassportNumber = worksheet.Cells[1, 7].Text?.Trim() ?? string.Empty;
                _mainViewModel.PassportIssueDate = worksheet.Cells[1, 8].Text?.Trim() ?? string.Empty;
                _mainViewModel.PassportIssuedBy = worksheet.Cells[1, 9].Text?.Trim() ?? string.Empty;
                _mainViewModel.Address = worksheet.Cells[1, 10].Text?.Trim() ?? string.Empty;
                _mainViewModel.Phone = worksheet.Cells[1, 11].Text?.Trim() ?? string.Empty;
                _mainViewModel.MedicalOrganization = worksheet.Cells[1, 12].Text?.Trim() ?? string.Empty;
                _mainViewModel.MedicalPolicy = worksheet.Cells[1, 13].Text?.Trim() ?? string.Empty;
                _mainViewModel.MedicalFacility = worksheet.Cells[1, 14].Text?.Trim() ?? string.Empty;
                _mainViewModel.Workplace = worksheet.Cells[1, 15].Text?.Trim() ?? string.Empty;
                _mainViewModel.OwnershipForm = worksheet.Cells[1, 16].Text?.Trim() ?? string.Empty;
                _mainViewModel.Okved = worksheet.Cells[1, 17].Text?.Trim() ?? string.Empty;
                _mainViewModel.WorkExperience = worksheet.Cells[1, 18].Text?.Trim() ?? string.Empty;

                var selectedClauses = new ObservableCollection<OrderClause>();
                int row = 1;
                while (!string.IsNullOrWhiteSpace(worksheet.Cells[row, 19].Text))
                {
                    var clauseText = worksheet.Cells[row, 19].Text?.Trim();
                    if (!string.IsNullOrEmpty(clauseText))
                    {
                        selectedClauses.Add(new OrderClause { ClauseText = clauseText });
                    }
                    row++;
                }
                _mainViewModel.SelectedOrderClauses = selectedClauses;

                int age = 0;
                if (!string.IsNullOrEmpty(_mainViewModel.DateOfBirth) && DateTime.TryParse(_mainViewModel.DateOfBirth, out DateTime birthDate))
                {
                    age = DateTime.Today.Year - birthDate.Year;
                    if (birthDate.Date > DateTime.Today.AddYears(-age)) age--;
                }

                var record = new Record
                {
                    FullName = _mainViewModel.FullName,
                    Position = _mainViewModel.Position,
                    DateOfBirth = _mainViewModel.DateOfBirth,
                    Age = age,
                    Gender = _mainViewModel.Gender,
                    OrderClauses = string.Join(", ", selectedClauses.Select(c => c.ClauseText)),
                    Snils = _mainViewModel.Snils,
                    MedicalPolicy = _mainViewModel.MedicalPolicy,
                    PassportSeries = _mainViewModel.PassportSeries,
                    PassportNumber = _mainViewModel.PassportNumber,
                    PassportIssueDate = _mainViewModel.PassportIssueDate,
                    PassportIssuedBy = _mainViewModel.PassportIssuedBy
                };

                Records.Clear();
                Records.Add(record);

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при чтении Excel: {ex.Message}\nStackTrace: {ex.StackTrace}");
                return false; // Возвращаем false вместо throw, чтобы обработать ошибку в UI
            }
        }
    }
}