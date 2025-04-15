using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;
using OfficeOpenXml;
using System.Threading.Tasks;
using System.IO;
using DocumentGenerator;

namespace DocumentGenerator.ViewModels
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null!)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private string? _fullName;
        public string? FullName
        {
            get => _fullName;
            set
            {
                if (_fullName != value)
                {
                    if (value != null && value.Length > 1000)
                        value = value.Substring(0, 1000);

                    _fullName = value;
                    OnPropertyChanged();
                    ValidateFullName();
                }
            }
        }

        private string _fullNameError = "";
        public string FullNameError
        {
            get => _fullNameError;
            set
            {
                if (_fullNameError != value)
                {
                    _fullNameError = value;
                    OnPropertyChanged();
                }
            }
        }

        private string? _position;
        public string? Position
        {
            get => _position;
            set
            {
                if (_position != value)
                {
                    if (value != null && value.Length > 1000)
                        value = value.Substring(0, 1000);

                    _position = value;
                    OnPropertyChanged();
                    ValidatePosition();
                }
            }
        }

        private string _positionError = "";
        public string PositionError
        {
            get => _positionError;
            set
            {
                if (_positionError != value)
                {
                    _positionError = value;
                    OnPropertyChanged();
                }
            }
        }

        private string? _dateOfBirth;
        public string? DateOfBirth
        {
            get => _dateOfBirth;
            set
            {
                if (_dateOfBirth != value)
                {
                    _dateOfBirth = value?.Replace(',', '.');
                    OnPropertyChanged();
                    ValidateDateOfBirth();
                }
            }
        }

        private string _dateOfBirthError = "";
        public string DateOfBirthError
        {
            get => _dateOfBirthError;
            set
            {
                if (_dateOfBirthError != value)
                {
                    _dateOfBirthError = value;
                    OnPropertyChanged();
                }
            }
        }

        private string? _gender;
        public string? Gender
        {
            get => _gender;
            set
            {
                if (_gender != value)
                {
                    _gender = value;
                    OnPropertyChanged();
                    ValidateGender();
                }
            }
        }

        private string _genderError = "";
        public string GenderError
        {
            get => _genderError;
            set
            {
                if (_genderError != value)
                {
                    _genderError = value;
                    OnPropertyChanged();
                }
            }
        }

        private string? _orderClause;
        public string? OrderClause
        {
            get => _orderClause;
            set
            {
                if (_orderClause != value)
                {
                    _orderClause = value;
                    OnPropertyChanged();
                    ValidateOrderClause();
                }
            }
        }

        private string _orderClauseError = "";
        public string OrderClauseError
        {
            get => _orderClauseError;
            set
            {
                if (_orderClauseError != value)
                {
                    _orderClauseError = value;
                    OnPropertyChanged();
                }
            }
        }

        private string? _snils;
        public string? Snils
        {
            get => _snils;
            set
            {
                if (_snils != value)
                {
                    _snils = value;
                    OnPropertyChanged();
                    ValidateSnils();
                }
            }
        }

        private string _snilsError = "";
        public string SnilsError
        {
            get => _snilsError;
            set
            {
                if (_snilsError != value)
                {
                    _snilsError = value;
                    OnPropertyChanged();
                }
            }
        }

        private string? _passportSeries;
        public string? PassportSeries
        {
            get => _passportSeries;
            set
            {
                if (_passportSeries != value)
                {
                    _passportSeries = value;
                    OnPropertyChanged();
                    ValidatePassportSeries();
                }
            }
        }

        private string _passportSeriesError = "";
        public string PassportSeriesError
        {
            get => _passportSeriesError;
            set
            {
                if (_passportSeriesError != value)
                {
                    _passportSeriesError = value;
                    OnPropertyChanged();
                }
            }
        }

        private string? _passportNumber;
        public string? PassportNumber
        {
            get => _passportNumber;
            set
            {
                if (_passportNumber != value)
                {
                    _passportNumber = value;
                    OnPropertyChanged();
                    ValidatePassportNumber();
                }
            }
        }

        private string _passportNumberError = "";
        public string PassportNumberError
        {
            get => _passportNumberError;
            set
            {
                if (_passportNumberError != value)
                {
                    _passportNumberError = value;
                    OnPropertyChanged();
                }
            }
        }

        private string? _passportIssueDate;
        public string? PassportIssueDate
        {
            get => _passportIssueDate;
            set
            {
                if (_passportIssueDate != value)
                {
                    _passportIssueDate = value?.Replace(',', '.');
                    OnPropertyChanged();
                    ValidatePassportIssueDate();
                }
            }
        }

        private string _passportIssueDateError = "";
        public string PassportIssueDateError
        {
            get => _passportIssueDateError;
            set
            {
                if (_passportIssueDateError != value)
                {
                    _passportIssueDateError = value;
                    OnPropertyChanged();
                }
            }
        }

        private string? _passportIssuedBy;
        public string? PassportIssuedBy
        {
            get => _passportIssuedBy;
            set
            {
                if (_passportIssuedBy != value)
                {
                    if (value != null && value.Length > 1000)
                        value = value.Substring(0, 1000);

                    _passportIssuedBy = value;
                    OnPropertyChanged();
                    ValidatePassportIssuedBy();
                }
            }
        }

        private string _passportIssuedByError = "";
        public string PassportIssuedByError
        {
            get => _passportIssuedByError;
            set
            {
                if (_passportIssuedByError != value)
                {
                    _passportIssuedByError = value;
                    OnPropertyChanged();
                }
            }
        }

        private string? _medicalPolicy;
        public string? MedicalPolicy
        {
            get => _medicalPolicy;
            set
            {
                if (_medicalPolicy != value)
                {
                    _medicalPolicy = value;
                    OnPropertyChanged();
                    ValidateMedicalPolicy();
                }
            }
        }

        private string _medicalPolicyError = "";
        public string MedicalPolicyError
        {
            get => _medicalPolicyError;
            set
            {
                if (_medicalPolicyError != value)
                {
                    _medicalPolicyError = value;
                    OnPropertyChanged();
                }
            }
        }

        public List<string> GenderOptions { get; }
        public List<string> OrderClauses { get; }

        public MainWindowViewModel()
        {
            GenderOptions = new List<string> { "Мужской", "Женский" };
            OrderClauses = Enumerable.Range(1, 27).Select(i => $"Пункт {i}").ToList();
        }

        public void ValidateFullName()
        {
            FullNameError = string.IsNullOrWhiteSpace(FullName) ? "ФИО не может быть пустым" : "";
        }

        public void ValidatePosition()
        {
            PositionError = string.IsNullOrWhiteSpace(Position) ? "Должность не может быть пустой" : "";
        }

        public void ValidateDateOfBirth()
        {
            if (string.IsNullOrWhiteSpace(DateOfBirth) || DateOfBirth.Replace("_", "").Replace(".", "").Trim().Length == 0)
            {
                DateOfBirthError = "Дата рождения не может быть пустой";
                return;
            }

            if (!Regex.IsMatch(DateOfBirth, @"^\d{2}\.\d{2}\.\d{4}$") || !DateTime.TryParseExact(DateOfBirth, "dd.MM.yyyy", null, System.Globalization.DateTimeStyles.None, out var dob) || dob > DateTime.Now)
                DateOfBirthError = "Дата рождения должна быть в формате ДД.ММ.ГГГГ и не позднее текущей даты";
            else
                DateOfBirthError = "";
        }

        public void ValidateGender()
        {
            GenderError = string.IsNullOrEmpty(Gender) ? "Выберите пол" : "";
        }

        public void ValidateOrderClause()
        {
            OrderClauseError = string.IsNullOrEmpty(OrderClause) ? "Выберите пункт приказа" : "";
        }

        public void ValidateSnils()
        {
            if (string.IsNullOrWhiteSpace(Snils) || Snils.Replace("_", "").Replace("-", "").Trim().Length == 0)
            {
                SnilsError = "СНИЛС не может быть пустым";
                return;
            }

            if (!Regex.IsMatch(Snils, @"^\d{3}-\d{3}-\d{3} \d{2}$"))
                SnilsError = "СНИЛС должен быть в формате XXX-XXX-XXX XX";
            else
                SnilsError = "";
        }

        public void ValidatePassportSeries()
        {
            if (string.IsNullOrWhiteSpace(PassportSeries))
                PassportSeriesError = "Серия паспорта не может быть пустой";
            else if (!Regex.IsMatch(PassportSeries, @"^[A-Z]{2}\d{2}$"))
                PassportSeriesError = "Серия паспорта должна быть в формате XXXX (например, AB12)";
            else
                PassportSeriesError = "";
        }

        public void ValidatePassportNumber()
        {
            if (string.IsNullOrWhiteSpace(PassportNumber))
                PassportNumberError = "Номер паспорта не может быть пустым";
            else if (!Regex.IsMatch(PassportNumber, @"^\d{6}$"))
                PassportNumberError = "Номер паспорта должен содержать ровно 6 цифр";
            else
                PassportNumberError = "";
        }

        public void ValidatePassportIssueDate()
        {
            if (string.IsNullOrWhiteSpace(PassportIssueDate) || PassportIssueDate.Replace("_", "").Replace(".", "").Trim().Length == 0)
            {
                PassportIssueDateError = "Дата выдачи паспорта не может быть пустой";
                return;
            }

            if (!Regex.IsMatch(PassportIssueDate, @"^\d{2}\.\d{2}\.\d{4}$") || !DateTime.TryParseExact(PassportIssueDate, "dd.MM.yyyy", null, System.Globalization.DateTimeStyles.None, out var issueDate) || issueDate > DateTime.Now)
                PassportIssueDateError = "Дата выдачи должна быть в формате ДД.ММ.ГГГГ и не позднее текущей даты";
            else
                PassportIssueDateError = "";
        }

        public void ValidatePassportIssuedBy()
        {
            PassportIssuedByError = string.IsNullOrWhiteSpace(PassportIssuedBy) ? "Поле 'Кем выдан' не может быть пустым" : "";
        }

        public void ValidateMedicalPolicy()
        {
            if (!string.IsNullOrWhiteSpace(MedicalPolicy) && !Regex.IsMatch(MedicalPolicy, @"^\d{16}$"))
                MedicalPolicyError = "Полис ОМС должен содержать ровно 16 цифр";
            else
                MedicalPolicyError = "";
        }

        private bool IsValid()
        {
            ValidateFullName();
            ValidatePosition();
            ValidateDateOfBirth();
            ValidateGender();
            ValidateOrderClause();
            ValidateSnils();
            ValidatePassportSeries();
            ValidatePassportNumber();
            ValidatePassportIssueDate();
            ValidatePassportIssuedBy();
            ValidateMedicalPolicy();

            return string.IsNullOrEmpty(FullNameError) &&
                   string.IsNullOrEmpty(PositionError) &&
                   string.IsNullOrEmpty(DateOfBirthError) &&
                   string.IsNullOrEmpty(GenderError) &&
                   string.IsNullOrEmpty(OrderClauseError) &&
                   string.IsNullOrEmpty(SnilsError) &&
                   string.IsNullOrEmpty(PassportSeriesError) &&
                   string.IsNullOrEmpty(PassportNumberError) &&
                   string.IsNullOrEmpty(PassportIssueDateError) &&
                   string.IsNullOrEmpty(PassportIssuedByError) &&
                   string.IsNullOrEmpty(MedicalPolicyError);
        }

        public void OnSave()
        {
            ValidateFullName();
            ValidatePosition();
            ValidateDateOfBirth();
            ValidateGender();
            ValidateOrderClause();
            ValidateSnils();
            ValidatePassportSeries();
            ValidatePassportNumber();
            ValidatePassportIssueDate();
            ValidatePassportIssuedBy();
            ValidateMedicalPolicy();

            if (!IsValid())
            {
                return;
            }

            if (DateOfBirth == null)
                throw new InvalidOperationException("Дата рождения не может быть null");

            var dob = DateTime.ParseExact(DateOfBirth, "dd.MM.yyyy", null);
            int age = DateTime.Now.Year - dob.Year;
            if (DateTime.Now.DayOfYear < dob.DayOfYear) age--;

            string connectionString = "Data Source=database.db";
            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS Documents (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        FullName TEXT,
                        Position TEXT,
                        DateOfBirth TEXT,
                        Age INTEGER,
                        Gender TEXT,
                        OrderClause TEXT,
                        Snils TEXT,
                        PassportSeries TEXT,
                        PassportNumber TEXT,
                        PassportIssueDate TEXT,
                        PassportIssuedBy TEXT,
                        MedicalPolicy TEXT
                    )";
                command.ExecuteNonQuery();

                command.CommandText = @"
                    INSERT INTO Documents (FullName, Position, DateOfBirth, Age, Gender, OrderClause, Snils, PassportSeries, PassportNumber, PassportIssueDate, PassportIssuedBy, MedicalPolicy)
                    VALUES (@fullName, @position, @dateOfBirth, @age, @gender, @orderClause, @snils, @passportSeries, @passportNumber, @passportIssueDate, @passportIssuedBy, @medicalPolicy)";
                command.Parameters.AddWithValue("@fullName", FullName ?? "");
                command.Parameters.AddWithValue("@position", Position ?? "");
                command.Parameters.AddWithValue("@dateOfBirth", DateOfBirth ?? "");
                command.Parameters.AddWithValue("@age", age);
                command.Parameters.AddWithValue("@gender", Gender ?? "");
                command.Parameters.AddWithValue("@orderClause", OrderClause ?? "");
                command.Parameters.AddWithValue("@snils", Snils ?? "");
                command.Parameters.AddWithValue("@passportSeries", PassportSeries ?? "");
                command.Parameters.AddWithValue("@passportNumber", PassportNumber ?? "");
                command.Parameters.AddWithValue("@passportIssueDate", PassportIssueDate ?? "");
                command.Parameters.AddWithValue("@passportIssuedBy", PassportIssuedBy ?? "");
                command.Parameters.AddWithValue("@medicalPolicy", MedicalPolicy ?? "");
                command.ExecuteNonQuery();
            }

            var previewWindow = new PreviewWindow
            {
                DataContext = new PreviewViewModel
                {
                    FullName = FullName,
                    Position = Position,
                    Age = age,
                    Gender = Gender,
                    OrderClause = OrderClause,
                    Snils = Snils,
                    PassportSeries = PassportSeries,
                    PassportNumber = PassportNumber,
                    PassportIssueDate = PassportIssueDate,
                    PassportIssuedBy = PassportIssuedBy,
                    MedicalPolicy = MedicalPolicy
                }
            };
            Console.WriteLine("PreviewWindow создан и DataContext установлен");
            previewWindow.Show();
        }

        public async Task LoadFromExcel(string filePath)
        {
            try
            {
                using (var package = new ExcelPackage(new FileInfo(filePath)))
                {
                    var worksheet = package.Workbook.Worksheets[0]; // Первый лист
                    var rowCount = worksheet.Dimension.Rows;
                    var records = new List<ExcelDataViewModel.Record>();

                    // Начинаем со второй строки (первая строка — заголовки)
                    for (int row = 2; row <= rowCount; row++)
                    {
                        var record = new ExcelDataViewModel.Record
                        {
                            FullName = worksheet.Cells[row, 1].Text,        // ФИО
                            Position = worksheet.Cells[row, 2].Text,        // Должность
                            DateOfBirth = worksheet.Cells[row, 3].Text,     // Дата рождения
                            Age = int.TryParse(worksheet.Cells[row, 4].Text, out int age) ? age : 0, // Возраст
                            Gender = worksheet.Cells[row, 5].Text,          // Пол
                            OrderClause = worksheet.Cells[row, 6].Text,     // Пункты по приказу
                            Snils = worksheet.Cells[row, 7].Text,           // СНИЛС
                            MedicalPolicy = worksheet.Cells[row, 8].Text,   // Полис ОМС
                            PassportSeries = worksheet.Cells[row, 9].Text,  // Серия паспорта
                            PassportNumber = worksheet.Cells[row, 10].Text, // Номер паспорта
                            PassportIssueDate = worksheet.Cells[row, 11].Text, // Дата выдачи паспорта
                            PassportIssuedBy = worksheet.Cells[row, 12].Text   // Кем выдан
                        };

                        records.Add(record);
                    }

                    if (records.Count == 0)
                    {
                        Console.WriteLine("В Excel-файле нет корректных данных.");
                        return;
                    }

                    // Открываем новое окно для отображения данных из Excel
                    var excelDataWindow = new ExcelDataWindow
                    {
                        DataContext = new ExcelDataViewModel
                        {
                            Records = records
                        }
                    };
                    excelDataWindow.Show();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при загрузке Excel: {ex.Message}");
            }
        }
    }
}