using ReactiveUI;
using System;
using System.Collections.Generic;
using DocumentGenerator.Services;
using System.Linq;

namespace DocumentGenerator.ViewModels
{
    public class NewFormViewModel : ReactiveObject
    {
        private readonly NewFormPdfGenerator _pdfGenerator; private readonly IServiceProvider _serviceProvider;

        // Поля формы
        private string? _medicalSeries;
        public string? MedicalSeries
        {
            get => _medicalSeries;
            set => this.RaiseAndSetIfChanged(ref _medicalSeries, value);
        }

        private string? _medicalNumber;
        public string? MedicalNumber
        {
            get => _medicalNumber;
            set => this.RaiseAndSetIfChanged(ref _medicalNumber, value);
        }

        private string? _fullName;
        public string? FullName
        {
            get => _fullName;
            set => this.RaiseAndSetIfChanged(ref _fullName, value);
        }

        private string? _dateOfBirth;
        public string? DateOfBirth
        {
            get => _dateOfBirth;
            set => this.RaiseAndSetIfChanged(ref _dateOfBirth, value);
        }

        private string? _gender;
        public string? Gender
        {
            get => _gender;
            set => this.RaiseAndSetIfChanged(ref _gender, value);
        }

        public List<string> GenderOptions { get; } = new List<string> { "Мужской", "Женский" };

        private string? _passportSeries;
        public string? PassportSeries
        {
            get => _passportSeries;
            set => this.RaiseAndSetIfChanged(ref _passportSeries, value);
        }

        private string? _passportNumber;
        public string? PassportNumber
        {
            get => _passportNumber;
            set => this.RaiseAndSetIfChanged(ref _passportNumber, value);
        }

        private string? _passportIssuedBy;
        public string? PassportIssuedBy
        {
            get => _passportIssuedBy;
            set => this.RaiseAndSetIfChanged(ref _passportIssuedBy, value);
        }

        private string? _bloodGroup;
        public string? BloodGroup
        {
            get => _bloodGroup;
            set => this.RaiseAndSetIfChanged(ref _bloodGroup, value);
        }

        public List<string> BloodGroupOptions { get; } = new List<string> { "I", "II", "III", "IV" };

        private string? _rhFactor;
        public string? RhFactor
        {
            get => _rhFactor;
            set => this.RaiseAndSetIfChanged(ref _rhFactor, value);
        }

        public List<string> RhFactorOptions { get; } = new List<string> { "+", "-" };

        private string? _phone;
        public string? Phone
        {
            get => _phone;
            set => this.RaiseAndSetIfChanged(ref _phone, value);
        }

        private string? _address;
        public string? Address
        {
            get => _address;
            set => this.RaiseAndSetIfChanged(ref _address, value);
        }

        private string? _drivingExperience;
        public string? DrivingExperience
        {
            get => _drivingExperience;
            set => this.RaiseAndSetIfChanged(ref _drivingExperience, value);
        }

        private string? _snils;
        public string? Snils
        {
            get => _snils;
            set => this.RaiseAndSetIfChanged(ref _snils, value);
        }

        private string? _fluorography;
        public string? Fluorography
        {
            get => _fluorography;
            set => this.RaiseAndSetIfChanged(ref _fluorography, value);
        }

        private string? _gynecologist;
        public string? Gynecologist
        {
            get => _gynecologist;
            set => this.RaiseAndSetIfChanged(ref _gynecologist, value);
        }

        // Свойства ошибок
        private string _medicalSeriesError = "";
        public string MedicalSeriesError
        {
            get => _medicalSeriesError;
            set => this.RaiseAndSetIfChanged(ref _medicalSeriesError, value);
        }

        private string _medicalNumberError = "";
        public string MedicalNumberError
        {
            get => _medicalNumberError;
            set => this.RaiseAndSetIfChanged(ref _medicalNumberError, value);
        }

        private string _fullNameError = "";
        public string FullNameError
        {
            get => _fullNameError;
            set => this.RaiseAndSetIfChanged(ref _fullNameError, value);
        }

        private string _dateOfBirthError = "";
        public string DateOfBirthError
        {
            get => _dateOfBirthError;
            set => this.RaiseAndSetIfChanged(ref _dateOfBirthError, value);
        }

        private string _genderError = "";
        public string GenderError
        {
            get => _genderError;
            set => this.RaiseAndSetIfChanged(ref _genderError, value);
        }

        private string _passportSeriesError = "";
        public string PassportSeriesError
        {
            get => _passportSeriesError;
            set => this.RaiseAndSetIfChanged(ref _passportSeriesError, value);
        }

        private string _passportNumberError = "";
        public string PassportNumberError
        {
            get => _passportNumberError;
            set => this.RaiseAndSetIfChanged(ref _passportNumberError, value);
        }

        private string _passportIssuedByError = "";
        public string PassportIssuedByError
        {
            get => _passportIssuedByError;
            set => this.RaiseAndSetIfChanged(ref _passportIssuedByError, value);
        }

        private string _bloodGroupError = "";
        public string BloodGroupError
        {
            get => _bloodGroupError;
            set => this.RaiseAndSetIfChanged(ref _bloodGroupError, value);
        }

        private string _rhFactorError = "";
        public string RhFactorError
        {
            get => _rhFactorError;
            set => this.RaiseAndSetIfChanged(ref _rhFactorError, value);
        }

        private string _phoneError = "";
        public string PhoneError
        {
            get => _phoneError;
            set => this.RaiseAndSetIfChanged(ref _phoneError, value);
        }

        private string _addressError = "";
        public string AddressError
        {
            get => _addressError;
            set => this.RaiseAndSetIfChanged(ref _addressError, value);
        }

        private string _drivingExperienceError = "";
        public string DrivingExperienceError
        {
            get => _drivingExperienceError;
            set => this.RaiseAndSetIfChanged(ref _drivingExperienceError, value);
        }

        private string _snilsError = "";
        public string SnilsError
        {
            get => _snilsError;
            set => this.RaiseAndSetIfChanged(ref _snilsError, value);
        }

        private string _fluorographyError = "";
        public string FluorographyError
        {
            get => _fluorographyError;
            set => this.RaiseAndSetIfChanged(ref _fluorographyError, value);
        }

        private string _gynecologistError = "";
        public string GynecologistError
        {
            get => _gynecologistError;
            set => this.RaiseAndSetIfChanged(ref _gynecologistError, value);
        }

        public NewFormViewModel(NewFormPdfGenerator pdfGenerator, IServiceProvider serviceProvider)
        {
            _pdfGenerator = pdfGenerator;
            _serviceProvider = serviceProvider;
        }

        // Методы валидации
        public void ValidateMedicalSeries()
        {
            MedicalSeriesError = string.IsNullOrWhiteSpace(MedicalSeries) ? "Серия медицинского освидетельствования не может быть пустой" : "";
        }

        public void ValidateMedicalNumber()
        {
            MedicalNumberError = string.IsNullOrWhiteSpace(MedicalNumber) ? "Номер медицинского освидетельствования не может быть пустым" : "";
        }

        public void ValidateFullName()
        {
            if (string.IsNullOrWhiteSpace(FullName))
            {
                FullNameError = "ФИО не может быть пустым";
                return;
            }

            var parts = FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
            {
                FullNameError = "ФИО должно содержать минимум два слова";
                return;
            }

            FullNameError = "";
        }

        public void ValidateDateOfBirth()
        {
            if (string.IsNullOrWhiteSpace(DateOfBirth))
            {
                DateOfBirthError = "Дата рождения не может быть пустой";
                return;
            }

            if (!DateTime.TryParseExact(DateOfBirth, "dd.MM.yyyy", null, System.Globalization.DateTimeStyles.None, out var date))
            {
                DateOfBirthError = "Дата рождения должна быть в формате ДД.ММ.ГГГГ";
                return;
            }

            if (DateTime.Now.Year - date.Year < 14)
            {
                DateOfBirthError = "Возраст должен быть не менее 14 лет";
                return;
            }

            DateOfBirthError = "";
        }

        public void ValidateGender()
        {
            GenderError = string.IsNullOrWhiteSpace(Gender) ? "Пол должен быть выбран" : "";
        }

        public void ValidatePassportSeries()
        {
            if (string.IsNullOrWhiteSpace(PassportSeries))
            {
                PassportSeriesError = "Серия паспорта не может быть пустой";
                return;
            }

            PassportSeriesError = PassportSeries.Length == 4 && PassportSeries.All(char.IsDigit)
                ? ""
                : "Серия паспорта должна содержать 4 цифры";
        }

        public void ValidatePassportNumber()
        {
            if (string.IsNullOrWhiteSpace(PassportNumber))
            {
                PassportNumberError = "Номер паспорта не может быть пустым";
                return;
            }

            PassportNumberError = PassportNumber.Length == 6 && PassportNumber.All(char.IsDigit)
                ? ""
                : "Номер паспорта должен содержать 6 цифр";
        }

        public void ValidatePassportIssuedBy()
        {
            PassportIssuedByError = string.IsNullOrWhiteSpace(PassportIssuedBy) ? "Кем выдан паспорт не может быть пустым" : "";
        }

        public void ValidateBloodGroup()
        {
            BloodGroupError = string.IsNullOrWhiteSpace(BloodGroup) ? "Группа крови должна быть выбрана" : "";
        }

        public void ValidateRhFactor()
        {
            RhFactorError = string.IsNullOrWhiteSpace(RhFactor) ? "Резус-фактор должен быть выбран" : "";
        }

        public void ValidatePhone()
        {
            if (string.IsNullOrWhiteSpace(Phone))
            {
                PhoneError = "Телефон не может быть пустым";
                return;
            }

            var digits = Phone.Replace("+", "").Replace(" ", "").Replace("(", "").Replace(")", "").Replace("-", "");
            if (digits.Length != 11 || !digits.All(char.IsDigit) || (digits[0] != '7' && digits[0] != '8'))
            {
                PhoneError = "Телефон должен быть в формате +X (XXX) XXX-XX-XX, начинаться с +7 или +8";
                return;
            }

            PhoneError = "";
        }

        public void ValidateAddress()
        {
            if (string.IsNullOrWhiteSpace(Address))
            {
                AddressError = "Адрес не может быть пустым";
                return;
            }

            var parts = Address.Split(',', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 3)
            {
                AddressError = "Адрес должен содержать город, улицу и дом (например, Москва, ул. Ленина, д. 5)";
                return;
            }

            AddressError = "";
        }

        public void ValidateDrivingExperience()
        {
            if (string.IsNullOrWhiteSpace(DrivingExperience))
            {
                DrivingExperienceError = "Водительский стаж не может быть пустым";
                return;
            }

            if (!int.TryParse(DrivingExperience, out int years) || years < 0 || years > 80)
            {
                DrivingExperienceError = "Водительский стаж должен быть числом от 0 до 80 лет";
                return;
            }

            DrivingExperienceError = "";
        }

        public void ValidateSnils()
        {
            if (string.IsNullOrWhiteSpace(Snils))
            {
                SnilsError = "СНИЛС не может быть пустым";
                return;
            }

            var digits = Snils.Replace("-", "").Replace(" ", "");
            if (digits.Length != 11 || !digits.All(char.IsDigit))
            {
                SnilsError = "СНИЛС должен содержать 11 цифр";
                return;
            }

            int checksum = int.Parse(digits.Substring(9, 2));
            int sum = 0;
            for (int i = 0; i < 9; i++)
            {
                sum += int.Parse(digits[i].ToString()) * (9 - i);
            }
            int expectedChecksum = sum % 101;
            if (expectedChecksum == 100) expectedChecksum = 0;

            SnilsError = checksum == expectedChecksum ? "" : "Неверная контрольная сумма СНИЛС";
        }

        public void ValidateFluorography()
        {
            FluorographyError = string.IsNullOrWhiteSpace(Fluorography) ? "Флюорография не может быть пустой" : "";
        }

        public void ValidateGynecologist()
        {
            if (Gender == "Женский" && string.IsNullOrWhiteSpace(Gynecologist))
            {
                GynecologistError = "Для женщин данные гинеколога обязательны";
                return;
            }

            GynecologistError = "";
        }

        public void OnSave()
        {
            // Выполняем валидацию
            ValidateMedicalSeries();
            ValidateMedicalNumber();
            ValidateFullName();
            ValidateDateOfBirth();
            ValidateGender();
            ValidatePassportSeries();
            ValidatePassportNumber();
            ValidatePassportIssuedBy();
            ValidateBloodGroup();
            ValidateRhFactor();
            ValidatePhone();
            ValidateAddress();
            ValidateDrivingExperience();
            ValidateSnils();
            ValidateFluorography();
            ValidateGynecologist();

            // Проверяем, есть ли ошибки валидации
            if (new[] { MedicalSeriesError, MedicalNumberError, FullNameError, DateOfBirthError, GenderError,
            PassportSeriesError, PassportNumberError, PassportIssuedByError, BloodGroupError, RhFactorError,
            PhoneError, AddressError, DrivingExperienceError, SnilsError, FluorographyError, GynecologistError }
                .Any(error => !string.IsNullOrEmpty(error)))
            {
                return; // Если есть ошибки, прерываем выполнение
            }

            // Собираем данные пользователя в словарь
            var userData = new Dictionary<string, string>
        {
            { "MedicalSeries", MedicalSeries ?? "" },
            { "MedicalNumber", MedicalNumber ?? "" },
            { "FullName", FullName ?? "" },
            { "DateOfBirth", DateOfBirth ?? "" },
            { "Gender", Gender ?? "" },
            { "PassportSeries", PassportSeries ?? "" },
            { "PassportNumber", PassportNumber ?? "" },
            { "PassportIssuedBy", PassportIssuedBy ?? "" },
            { "BloodGroup", BloodGroup ?? "" },
            { "RhFactor", RhFactor ?? "" },
            { "Phone", Phone ?? "" },
            { "Address", Address ?? "" },
            { "DrivingExperience", DrivingExperience ?? "" },
            { "Snils", Snils ?? "" },
            { "Fluorography", Fluorography ?? "" },
            { "Gynecologist", Gynecologist ?? "" }
        };

            // Логика сохранения будет перенесена в NewForm.axaml.cs
        }

        public void Cleanup()
        {
            // Здесь можно добавить логику очистки, если потребуется
        }
    }

}