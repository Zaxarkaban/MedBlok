using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;

namespace DocumentGenerator.ViewModels
{
    public class MainWindowViewModel
    {
        public string? FullName { get; set; } = "";
        public string FullNameError { get; set; } = "";
        public string? Position { get; set; } = "";
        public string PositionError { get; set; } = "";
        public string? DateOfBirth { get; set; } = "";
        public string DateOfBirthError { get; set; } = "";
        public string? Gender { get; set; } = "";
        public string GenderError { get; set; } = "";
        public string? OrderClause { get; set; } = "";
        public string OrderClauseError { get; set; } = "";
        public string? Snils { get; set; } = "";
        public string SnilsError { get; set; } = "";
        public string? PassportSeries { get; set; } = "";
        public string PassportSeriesError { get; set; } = "";
        public string? PassportNumber { get; set; } = "";
        public string PassportNumberError { get; set; } = "";
        public string? PassportIssueDate { get; set; } = "";
        public string PassportIssueDateError { get; set; } = "";
        public string? PassportIssuedBy { get; set; } = "";
        public string PassportIssuedByError { get; set; } = "";
        public string? MedicalPolicy { get; set; } = "";
        public string MedicalPolicyError { get; set; } = "";

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
            if (string.IsNullOrWhiteSpace(DateOfBirth))
                DateOfBirthError = "Дата рождения не может быть пустой";
            else if (!Regex.IsMatch(DateOfBirth, @"^\d{2}\.\d{2}\.\d{4}$") || !DateTime.TryParseExact(DateOfBirth, "dd.MM.yyyy", null, System.Globalization.DateTimeStyles.None, out var dob) || dob > DateTime.Now)
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
            if (string.IsNullOrWhiteSpace(Snils))
                SnilsError = "СНИЛС не может быть пустым";
            else if (!Regex.IsMatch(Snils, @"^\d{3}-\d{3}-\d{3} \d{2}$"))
                SnilsError = "СНИЛС должен быть в формате XXX-XXX-XXX XX";
            else
                SnilsError = "";
        }

        public void ValidatePassportSeries()
        {
            if (string.IsNullOrWhiteSpace(PassportSeries))
                PassportSeriesError = "Серия паспорта не может быть пустой";
            else if (!Regex.IsMatch(PassportSeries, @"^[A-Z]{2} \d{2}$"))
                PassportSeriesError = "Серия паспорта должна быть в формате XX XX (например, AB 12)";
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
            if (string.IsNullOrWhiteSpace(PassportIssueDate))
                PassportIssueDateError = "Дата выдачи паспорта не может быть пустой";
            else if (!Regex.IsMatch(PassportIssueDate, @"^\d{2}\.\d{2}\.\d{4}$") || !DateTime.TryParseExact(PassportIssueDate, "dd.MM.yyyy", null, System.Globalization.DateTimeStyles.None, out var issueDate) || issueDate > DateTime.Now)
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
            if (!IsValid())
                return;

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
                    INSERT INTO Documents (FullName, Position, Age, Gender, OrderClause, Snils, PassportSeries, PassportNumber, PassportIssueDate, PassportIssuedBy, MedicalPolicy)
                    VALUES (@fullName, @position, @age, @gender, @orderClause, @snils, @passportSeries, @passportNumber, @passportIssueDate, @passportIssuedBy, @medicalPolicy)";
                command.Parameters.AddWithValue("@fullName", FullName ?? "");
                command.Parameters.AddWithValue("@position", Position ?? "");
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
    }
}