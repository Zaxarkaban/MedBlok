using ReactiveUI;
using Avalonia;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using DocumentGenerator.Models;
using DocumentGenerator.Services;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Avalonia.Controls.ApplicationLifetimes;

namespace DocumentGenerator.ViewModels
{
    public class DataEntryViewModel : ReactiveObject
    {
        private string _fullName = "";
        private string _position = "";
        private string _dateOfBirth = "";
        private string _gender = "";
        private string _snils = "";
        private string _passportSeries = "";
        private string _passportNumber = "";
        private string _passportIssueDate = "";
        private string _passportIssuedBy = "";
        private string _medicalPolicy = "";
        private string _address = "";
        private string _phone = "";
        private string _medicalOrganization = "";
        private string _medicalFacility = "";
        private string _workplace = "";
        private string _ownershipForm = "";
        private string _okved = "";
        private string _workExperience = "";

        private string _fullNameError = "";
        private string _positionError = "";
        private string _dateOfBirthError = "";
        private string _genderError = "";
        private string _snilsError = "";
        private string _passportSeriesError = "";
        private string _passportNumberError = "";
        private string _passportIssueDateError = "";
        private string _passportIssuedByError = "";
        private string _medicalPolicyError = "";
        private string _addressError = "";
        private string _phoneError = "";
        private string _medicalOrganizationError = "";
        private string _medicalFacilityError = "";
        private string _workplaceError = "";
        private string _ownershipFormError = "";
        private string _okvedError = "";
        private string _workExperienceError = "";
        private string _selectedOrderClausesError = "";

        private ObservableCollection<string> _selectedOrderClauses = new ObservableCollection<string>();

        private readonly DocumentService _documentService;
        private readonly IServiceProvider _serviceProvider;

        public ICommand SaveCommand { get; }
        public ICommand LoadExcelCommand { get; }

        public DataEntryViewModel(DocumentService documentService, IServiceProvider serviceProvider)
        {
            _documentService = documentService;
            _serviceProvider = serviceProvider;
            GenderOptions = new List<string> { "Мужской", "Женский" };
            OwnershipFormOptions = new List<string> { "ООО", "ИП", "АО", "ПАО" };
            OrderClauseOptions = Dictionaries.OrderClauseDataMap.Keys.ToList();

            SaveCommand = ReactiveCommand.CreateFromTask(SaveAsync);
            LoadExcelCommand = ReactiveCommand.CreateFromTask(LoadExcelAsync);
        }

        public string FullName
        {
            get => _fullName;
            set => this.RaiseAndSetIfChanged(ref _fullName, value);
        }

        public string Position
        {
            get => _position;
            set => this.RaiseAndSetIfChanged(ref _position, value);
        }

        public string DateOfBirth
        {
            get => _dateOfBirth;
            set => this.RaiseAndSetIfChanged(ref _dateOfBirth, value);
        }

        public string Gender
        {
            get => _gender;
            set => this.RaiseAndSetIfChanged(ref _gender, value);
        }

        public string Snils
        {
            get => _snils;
            set => this.RaiseAndSetIfChanged(ref _snils, value);
        }

        public string PassportSeries
        {
            get => _passportSeries;
            set => this.RaiseAndSetIfChanged(ref _passportSeries, value);
        }

        public string PassportNumber
        {
            get => _passportNumber;
            set => this.RaiseAndSetIfChanged(ref _passportNumber, value);
        }

        public string PassportIssueDate
        {
            get => _passportIssueDate;
            set => this.RaiseAndSetIfChanged(ref _passportIssueDate, value);
        }

        public string PassportIssuedBy
        {
            get => _passportIssuedBy;
            set => this.RaiseAndSetIfChanged(ref _passportIssuedBy, value);
        }

        public string MedicalPolicy
        {
            get => _medicalPolicy;
            set => this.RaiseAndSetIfChanged(ref _medicalPolicy, value);
        }

        public string Address
        {
            get => _address;
            set => this.RaiseAndSetIfChanged(ref _address, value);
        }

        public string Phone
        {
            get => _phone;
            set => this.RaiseAndSetIfChanged(ref _phone, value);
        }

        public string MedicalOrganization
        {
            get => _medicalOrganization;
            set => this.RaiseAndSetIfChanged(ref _medicalOrganization, value);
        }

        public string MedicalFacility
        {
            get => _medicalFacility;
            set => this.RaiseAndSetIfChanged(ref _medicalFacility, value);
        }

        public string Workplace
        {
            get => _workplace;
            set => this.RaiseAndSetIfChanged(ref _workplace, value);
        }

        public string OwnershipForm
        {
            get => _ownershipForm;
            set => this.RaiseAndSetIfChanged(ref _ownershipForm, value);
        }

        public string Okved
        {
            get => _okved;
            set => this.RaiseAndSetIfChanged(ref _okved, value);
        }

        public string WorkExperience
        {
            get => _workExperience;
            set => this.RaiseAndSetIfChanged(ref _workExperience, value);
        }

        public ObservableCollection<string> SelectedOrderClauses
        {
            get => _selectedOrderClauses;
            set => this.RaiseAndSetIfChanged(ref _selectedOrderClauses, value);
        }

        public List<string> GenderOptions { get; }
        public List<string> OwnershipFormOptions { get; }
        public List<string> OrderClauseOptions { get; }

        public string FullNameError
        {
            get => _fullNameError;
            set => this.RaiseAndSetIfChanged(ref _fullNameError, value);
        }

        public string PositionError
        {
            get => _positionError;
            set => this.RaiseAndSetIfChanged(ref _positionError, value);
        }

        public string DateOfBirthError
        {
            get => _dateOfBirthError;
            set => this.RaiseAndSetIfChanged(ref _dateOfBirthError, value);
        }

        public string GenderError
        {
            get => _genderError;
            set => this.RaiseAndSetIfChanged(ref _genderError, value);
        }

        public string SnilsError
        {
            get => _snilsError;
            set => this.RaiseAndSetIfChanged(ref _snilsError, value);
        }

        public string PassportSeriesError
        {
            get => _passportSeriesError;
            set => this.RaiseAndSetIfChanged(ref _passportSeriesError, value);
        }

        public string PassportNumberError
        {
            get => _passportNumberError;
            set => this.RaiseAndSetIfChanged(ref _passportNumberError, value);
        }

        public string PassportIssueDateError
        {
            get => _passportIssueDateError;
            set => this.RaiseAndSetIfChanged(ref _passportIssueDateError, value);
        }

        public string PassportIssuedByError
        {
            get => _passportIssuedByError;
            set => this.RaiseAndSetIfChanged(ref _passportIssuedByError, value);
        }

        public string MedicalPolicyError
        {
            get => _medicalPolicyError;
            set => this.RaiseAndSetIfChanged(ref _medicalPolicyError, value);
        }

        public string AddressError
        {
            get => _addressError;
            set => this.RaiseAndSetIfChanged(ref _addressError, value);
        }

        public string PhoneError
        {
            get => _phoneError;
            set => this.RaiseAndSetIfChanged(ref _phoneError, value);
        }

        public string MedicalOrganizationError
        {
            get => _medicalOrganizationError;
            set => this.RaiseAndSetIfChanged(ref _medicalOrganizationError, value);
        }

        public string MedicalFacilityError
        {
            get => _medicalFacilityError;
            set => this.RaiseAndSetIfChanged(ref _medicalFacilityError, value);
        }

        public string WorkplaceError
        {
            get => _workplaceError;
            set => this.RaiseAndSetIfChanged(ref _workplaceError, value);
        }

        public string OwnershipFormError
        {
            get => _ownershipFormError;
            set => this.RaiseAndSetIfChanged(ref _ownershipFormError, value);
        }

        public string OkvedError
        {
            get => _okvedError;
            set => this.RaiseAndSetIfChanged(ref _okvedError, value);
        }

        public string WorkExperienceError
        {
            get => _workExperienceError;
            set => this.RaiseAndSetIfChanged(ref _workExperienceError, value);
        }

        public string SelectedOrderClausesError
        {
            get => _selectedOrderClausesError;
            set => this.RaiseAndSetIfChanged(ref _selectedOrderClausesError, value);
        }

        public List<string> GetDoctorsForSelectedClauses()
        {
            var doctors = new List<string>();
            foreach (var clause in SelectedOrderClauses)
            {
                if (Dictionaries.OrderClauseDataMap.TryGetValue(clause, out var clauseData))
                {
                    doctors.AddRange(clauseData.Doctors);
                }
            }
            return doctors.Distinct().ToList();
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

        public void ValidatePosition()
        {
            PositionError = string.IsNullOrWhiteSpace(Position) ? "Должность не может быть пустой" : "";
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

        public void ValidatePassportIssueDate()
        {
            if (string.IsNullOrWhiteSpace(PassportIssueDate))
            {
                PassportIssueDateError = "Дата выдачи паспорта не может быть пустой";
                return;
            }

            if (!DateTime.TryParseExact(PassportIssueDate, "dd.MM.yyyy", null, System.Globalization.DateTimeStyles.None, out var date))
            {
                PassportIssueDateError = "Дата выдачи паспорта должна быть в формате ДД.ММ.ГГГГ";
                return;
            }

            if (date > DateTime.Now)
            {
                PassportIssueDateError = "Дата выдачи паспорта не может быть в будущем";
                return;
            }

            PassportIssueDateError = "";
        }

        public void ValidatePassportIssuedBy()
        {
            PassportIssuedByError = string.IsNullOrWhiteSpace(PassportIssuedBy) ? "Кем выдан паспорт не может быть пустым" : "";
        }

        public void ValidateMedicalPolicy()
        {
            if (string.IsNullOrWhiteSpace(MedicalPolicy))
            {
                MedicalPolicyError = "";
                return;
            }

            MedicalPolicyError = MedicalPolicy.Length == 16 && MedicalPolicy.All(char.IsDigit)
                ? ""
                : "Полис ОМС должен содержать 16 цифр";
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

        public void ValidateMedicalOrganization()
        {
            MedicalOrganizationError = string.IsNullOrWhiteSpace(MedicalOrganization)
                ? "Наименование страховой медицинской организации не может быть пустым"
                : "";
        }

        public void ValidateMedicalFacility()
        {
            MedicalFacilityError = string.IsNullOrWhiteSpace(MedicalFacility)
                ? "Наблюдается ЛПУ должен быть указан"
                : "";
        }

        public void ValidateWorkplace()
        {
            WorkplaceError = string.IsNullOrWhiteSpace(Workplace)
                ? "Место работы не может быть пустым"
                : "";
        }

        public void ValidateOwnershipForm()
        {
            OwnershipFormError = string.IsNullOrWhiteSpace(OwnershipForm)
                ? "Форма собственности должна быть выбрана"
                : "";
        }

        public void ValidateOkved()
        {
            if (string.IsNullOrWhiteSpace(Okved))
            {
                OkvedError = "ОКВЭД не может быть пустым";
                return;
            }

            var parts = Okved.Split('.');
            if (parts.Length < 2 || parts.Length > 3 || !parts.All(p => p.Length == 2 && p.All(char.IsDigit)))
            {
                OkvedError = "ОКВЭД должен быть в формате XX.XX или XX.XX.XX";
                return;
            }

            int firstPart = int.Parse(parts[0]);
            int secondPart = int.Parse(parts[1]);
            if (firstPart == 0 || secondPart == 0)
            {
                OkvedError = "ОКВЭД не может содержать нулевые части (XX и XX должны быть от 01 до 99)";
                return;
            }

            OkvedError = "";
        }

        public void ValidateWorkExperience()
        {
            if (string.IsNullOrWhiteSpace(WorkExperience))
            {
                WorkExperienceError = "Стаж работы не может быть пустым";
                return;
            }

            var digits = WorkExperience.Replace(" лет", "");
            if (!int.TryParse(digits, out int years) || years < 0 || years > 80)
            {
                WorkExperienceError = "Стаж работы должен быть числом от 0 до 80 лет";
                return;
            }

            WorkExperienceError = "";
        }

        private int CalculateAge()
        {
            if (string.IsNullOrEmpty(DateOfBirth)) return 0;

            if (!DateTime.TryParseExact(DateOfBirth, "dd.MM.yyyy", null, System.Globalization.DateTimeStyles.None, out var dob))
                return 0;

            var today = DateTime.Today;
            int age = today.Year - dob.Year;
            if (dob.Date > today.AddYears(-age)) age--;
            return age;
        }

        public void ValidateSelectedOrderClauses()
        {
            SelectedOrderClausesError = SelectedOrderClauses.Count == 0
                ? "Необходимо выбрать хотя бы один пункт вредности"
                : "";
        }

        private async Task SaveAsync()
        {
            ValidateFullName();
            ValidatePosition();
            ValidateDateOfBirth();
            ValidateGender();
            ValidateSnils();
            ValidatePassportSeries();
            ValidatePassportNumber();
            ValidatePassportIssueDate();
            ValidatePassportIssuedBy();
            ValidateMedicalPolicy();
            ValidateAddress();
            ValidatePhone();
            ValidateMedicalOrganization();
            ValidateMedicalFacility();
            ValidateWorkplace();
            ValidateOwnershipForm();
            ValidateOkved();
            ValidateWorkExperience();
            ValidateSelectedOrderClauses();

            if (new[] { FullNameError, PositionError, DateOfBirthError, GenderError, SnilsError, PassportSeriesError,
                PassportNumberError, PassportIssueDateError, PassportIssuedByError, MedicalPolicyError,
                AddressError, PhoneError, MedicalOrganizationError, MedicalFacilityError, WorkplaceError,
                OwnershipFormError, OkvedError, WorkExperienceError, SelectedOrderClausesError }
                .Any(error => !string.IsNullOrEmpty(error)))
            {
                return;
            }

            int age = CalculateAge();
            bool isFemale = Gender == "Женский";
            bool isOver40 = age > 40;

            var userData = new Dictionary<string, string>
            {
                { "FullName", FullName },
                { "Position", Position },
                { "DateOfBirth", DateOfBirth },
                { "Gender", Gender },
                { "Snils", Snils },
                { "PassportSeries", PassportSeries },
                { "PassportNumber", PassportNumber },
                { "PassportIssueDate", PassportIssueDate },
                { "PassportIssuedBy", PassportIssuedBy },
                { "MedicalPolicy", MedicalPolicy },
                { "Address", Address },
                { "Phone", Phone },
                { "MedicalOrganization", MedicalOrganization },
                { "MedicalFacility", MedicalFacility },
                { "Workplace", Workplace },
                { "OwnershipForm", OwnershipForm },
                { "Okved", Okved },
                { "WorkExperience", WorkExperience },
                { "OrderClause", string.Join(", ", SelectedOrderClauses) }
            };

            var doctors = _documentService.GenerateDoctorsList(SelectedOrderClauses.ToList(), isOver40, isFemale);

            var window = Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
                ? desktop.MainWindow
                : throw new InvalidOperationException("Не удалось получить главное окно приложения");

            var saveFileDialog = new SaveFileDialog
            {
                Title = "Сохранить PDF-документ",
                Filters = new List<FileDialogFilter>
                {
                    new FileDialogFilter { Name = "PDF Files", Extensions = { "pdf" } }
                },
                DefaultExtension = "pdf",
                InitialFileName = $"{SanitizeFileName(FullName ?? "Document")}.pdf"
            };

            var result = await saveFileDialog.ShowAsync(window);
            if (!string.IsNullOrEmpty(result))
            {
                var pdfGenerator = new PdfGenerator(this);
                string templatePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Template.pdf");
                pdfGenerator.GeneratePdf(result, templatePath);
            }
        }

        private async Task LoadExcelAsync()
        {
            var window = Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
                ? desktop.MainWindow
                : throw new InvalidOperationException("Не удалось получить главное окно приложения");

            var openFileDialog = new OpenFileDialog
            {
                Title = "Выбрать Excel-файл",
                Filters = new List<FileDialogFilter>
                {
                    new FileDialogFilter { Name = "Excel Files", Extensions = { "xlsx", "xls" } },
                    new FileDialogFilter { Name = "All Files", Extensions = { "*" } }
                }
            };

            var result = await openFileDialog.ShowAsync(window);
            if (result != null && result.Length > 0)
            {
                var filePath = result[0];
                var viewModel = _serviceProvider.GetRequiredService<ExcelDataViewModel>();
                await viewModel.LoadFromExcel(filePath);
                var excelWindow = new ExcelDataWindow
                {
                    DataContext = viewModel
                };
                await excelWindow.ShowDialog(window);
            }
        }

        private string SanitizeFileName(string fileName)
        {
            foreach (var c in System.IO.Path.GetInvalidFileNameChars())
            {
                fileName = fileName.Replace(c, '_');
            }
            return fileName.Trim();
        }
    }
}