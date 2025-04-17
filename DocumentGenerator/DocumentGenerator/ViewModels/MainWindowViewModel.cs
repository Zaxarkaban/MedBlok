using DocumentGenerator.Data.Entities;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace DocumentGenerator.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        // Поля для свойств
        private string _fullName = string.Empty;
        private string _position = string.Empty;
        private string _dateOfBirth = string.Empty;
        private string _gender = string.Empty;
        private string _snils = string.Empty;
        private string _passportSeries = string.Empty;
        private string _passportNumber = string.Empty;
        private string _passportIssueDate = string.Empty;
        private string _passportIssuedBy = string.Empty;
        private string _address = string.Empty;
        private string _phone = string.Empty;
        private string _medicalOrganization = string.Empty;
        private string _medicalPolicy = string.Empty;
        private string _medicalFacility = string.Empty;
        private string _workplace = string.Empty;
        private string _ownershipForm = string.Empty;
        private string _okved = string.Empty;
        private string _workExperience = string.Empty;

        private string _fullNameError = string.Empty;
        private string _positionError = string.Empty;
        private string _dateOfBirthError = string.Empty;
        private string _genderError = string.Empty;
        private string _snilsError = string.Empty;
        private string _passportSeriesError = string.Empty;
        private string _passportNumberError = string.Empty;
        private string _passportIssueDateError = string.Empty;
        private string _passportIssuedByError = string.Empty;
        private string _addressError = string.Empty;
        private string _phoneError = string.Empty;
        private string _medicalOrganizationError = string.Empty;
        private string _medicalPolicyError = string.Empty;
        private string _medicalFacilityError = string.Empty;
        private string _workplaceError = string.Empty;
        private string _ownershipFormError = string.Empty;
        private string _okvedError = string.Empty;
        private string _workExperienceError = string.Empty;
        private string _selectedOrderClausesError = string.Empty;

        private ObservableCollection<OrderClause> _selectedOrderClauses = new ObservableCollection<OrderClause>();
        private ObservableCollection<string> _doctors = new ObservableCollection<string>();

        // Новые свойства для коллекций
        public ObservableCollection<string> GenderOptions { get; } = new ObservableCollection<string> { "Мужской", "Женский" };
        public ObservableCollection<string> OwnershipFormOptions { get; } = new ObservableCollection<string> { "ООО", "ИП", "АО" };
        public ObservableCollection<OrderClause> OrderClauses { get; } = new ObservableCollection<OrderClause>();

        // Свойства с уведомлением об изменении
        public string FullName
        {
            get => _fullName;
            set => SetProperty(ref _fullName, value);
        }

        public string FullNameError
        {
            get => _fullNameError;
            set => SetProperty(ref _fullNameError, value);
        }

        public string Position
        {
            get => _position;
            set => SetProperty(ref _position, value);
        }

        public string PositionError
        {
            get => _positionError;
            set => SetProperty(ref _positionError, value);
        }

        public string DateOfBirth
        {
            get => _dateOfBirth;
            set => SetProperty(ref _dateOfBirth, value);
        }

        public string DateOfBirthError
        {
            get => _dateOfBirthError;
            set => SetProperty(ref _dateOfBirthError, value);
        }

        public string Gender
        {
            get => _gender;
            set => SetProperty(ref _gender, value);
        }

        public string GenderError
        {
            get => _genderError;
            set => SetProperty(ref _genderError, value);
        }

        public string Snils
        {
            get => _snils;
            set => SetProperty(ref _snils, value);
        }

        public string SnilsError
        {
            get => _snilsError;
            set => SetProperty(ref _snilsError, value);
        }

        public string PassportSeries
        {
            get => _passportSeries;
            set => SetProperty(ref _passportSeries, value);
        }

        public string PassportSeriesError
        {
            get => _passportSeriesError;
            set => SetProperty(ref _passportSeriesError, value);
        }

        public string PassportNumber
        {
            get => _passportNumber;
            set => SetProperty(ref _passportNumber, value);
        }

        public string PassportNumberError
        {
            get => _passportNumberError;
            set => SetProperty(ref _passportNumberError, value);
        }

        public string PassportIssueDate
        {
            get => _passportIssueDate;
            set => SetProperty(ref _passportIssueDate, value);
        }

        public string PassportIssueDateError
        {
            get => _passportIssueDateError;
            set => SetProperty(ref _passportIssueDateError, value);
        }

        public string PassportIssuedBy
        {
            get => _passportIssuedBy;
            set => SetProperty(ref _passportIssuedBy, value);
        }

        public string PassportIssuedByError
        {
            get => _passportIssuedByError;
            set => SetProperty(ref _passportIssuedByError, value);
        }

        public string Address
        {
            get => _address;
            set => SetProperty(ref _address, value);
        }

        public string AddressError
        {
            get => _addressError;
            set => SetProperty(ref _addressError, value);
        }

        public string Phone
        {
            get => _phone;
            set => SetProperty(ref _phone, value);
        }

        public string PhoneError
        {
            get => _phoneError;
            set => SetProperty(ref _phoneError, value);
        }

        public string MedicalOrganization
        {
            get => _medicalOrganization;
            set => SetProperty(ref _medicalOrganization, value);
        }

        public string MedicalOrganizationError
        {
            get => _medicalOrganizationError;
            set => SetProperty(ref _medicalOrganizationError, value);
        }

        public string MedicalPolicy
        {
            get => _medicalPolicy;
            set => SetProperty(ref _medicalPolicy, value);
        }

        public string MedicalPolicyError
        {
            get => _medicalPolicyError;
            set => SetProperty(ref _medicalPolicyError, value);
        }

        public string MedicalFacility
        {
            get => _medicalFacility;
            set => SetProperty(ref _medicalFacility, value);
        }

        public string MedicalFacilityError
        {
            get => _medicalFacilityError;
            set => SetProperty(ref _medicalFacilityError, value);
        }

        public string Workplace
        {
            get => _workplace;
            set => SetProperty(ref _workplace, value);
        }

        public string WorkplaceError
        {
            get => _workplaceError;
            set => SetProperty(ref _workplaceError, value);
        }

        public string OwnershipForm
        {
            get => _ownershipForm;
            set => SetProperty(ref _ownershipForm, value);
        }

        public string OwnershipFormError
        {
            get => _ownershipFormError;
            set => SetProperty(ref _ownershipFormError, value);
        }

        public string Okved
        {
            get => _okved;
            set => SetProperty(ref _okved, value);
        }

        public string OkvedError
        {
            get => _okvedError;
            set => SetProperty(ref _okvedError, value);
        }

        public string WorkExperience
        {
            get => _workExperience;
            set => SetProperty(ref _workExperience, value);
        }

        public string WorkExperienceError
        {
            get => _workExperienceError;
            set => SetProperty(ref _workExperienceError, value);
        }

        public ObservableCollection<OrderClause> SelectedOrderClauses
        {
            get => _selectedOrderClauses;
            set => SetProperty(ref _selectedOrderClauses, value);
        }

        public string SelectedOrderClausesError
        {
            get => _selectedOrderClausesError;
            set => SetProperty(ref _selectedOrderClausesError, value);
        }

        public ObservableCollection<string> Doctors
        {
            get => _doctors;
            set => SetProperty(ref _doctors, value);
        }

        public void OnSave()
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
            ValidateAddress();
            ValidatePhone();
            ValidateMedicalOrganization();
            ValidateMedicalPolicy();
            ValidateMedicalFacility();
            ValidateWorkplace();
            ValidateOwnershipForm();
            ValidateOkved();
            ValidateWorkExperience();
            ValidateSelectedOrderClauses();
        }

        private void ValidateFullName() => FullNameError = string.IsNullOrWhiteSpace(FullName) ? "ФИО обязательно" : null;

        private void ValidatePosition() => PositionError = string.IsNullOrWhiteSpace(Position) ? "Должность обязательна" : null;

        private void ValidateDateOfBirth()
        {
            if (string.IsNullOrWhiteSpace(DateOfBirth))
            {
                DateOfBirthError = "Дата рождения обязательна";
            }
            else if (!DateTime.TryParse(DateOfBirth, out _))
            {
                DateOfBirthError = "Неверный формат даты (дд.мм.гггг)";
            }
            else
            {
                DateOfBirthError = null;
            }
        }

        private void ValidateGender() => GenderError = string.IsNullOrWhiteSpace(Gender) ? "Пол обязателен" : null;

        private void ValidateSnils()
        {
            if (string.IsNullOrWhiteSpace(Snils))
            {
                SnilsError = "СНИЛС обязателен";
            }
            else if (Snils.Replace("-", "").Replace(" ", "").Length != 11)
            {
                SnilsError = "СНИЛС должен содержать 11 цифр";
            }
            else
            {
                SnilsError = null;
            }
        }

        private void ValidatePassportSeries()
        {
            if (string.IsNullOrWhiteSpace(PassportSeries))
            {
                PassportSeriesError = "Серия паспорта обязательна";
            }
            else if (PassportSeries.Length != 4)
            {
                PassportSeriesError = "Серия паспорта должна содержать 4 цифры";
            }
            else
            {
                PassportSeriesError = null;
            }
        }

        private void ValidatePassportNumber()
        {
            if (string.IsNullOrWhiteSpace(PassportNumber))
            {
                PassportNumberError = "Номер паспорта обязателен";
            }
            else if (PassportNumber.Length != 6)
            {
                PassportNumberError = "Номер паспорта должен содержать 6 цифр";
            }
            else
            {
                PassportNumberError = null;
            }
        }

        private void ValidatePassportIssueDate()
        {
            if (string.IsNullOrWhiteSpace(PassportIssueDate))
            {
                PassportIssueDateError = "Дата выдачи паспорта обязательна";
            }
            else if (!DateTime.TryParse(PassportIssueDate, out _))
            {
                PassportIssueDateError = "Неверный формат даты (дд.мм.гггг)";
            }
            else
            {
                PassportIssueDateError = null;
            }
        }

        private void ValidatePassportIssuedBy() => PassportIssuedByError = string.IsNullOrWhiteSpace(PassportIssuedBy) ? "Кем выдан паспорт - обязательно" : null;

        private void ValidateAddress() => AddressError = string.IsNullOrWhiteSpace(Address) ? "Адрес обязателен" : null;

        private void ValidatePhone()
        {
            if (string.IsNullOrWhiteSpace(Phone))
            {
                PhoneError = "Телефон обязателен";
            }
            else if (Phone.Replace("+", "").Replace(" ", "").Replace("(", "").Replace(")", "").Replace("-", "").Length != 11)
            {
                PhoneError = "Телефон должен содержать 11 цифр";
            }
            else
            {
                PhoneError = null;
            }
        }

        private void ValidateMedicalOrganization() => MedicalOrganizationError = string.IsNullOrWhiteSpace(MedicalOrganization) ? "Медицинская организация обязательна" : null;

        private void ValidateMedicalPolicy()
        {
            if (string.IsNullOrWhiteSpace(MedicalPolicy))
            {
                MedicalPolicyError = "Медицинский полис обязателен";
            }
            else if (MedicalPolicy.Length != 16)
            {
                MedicalPolicyError = "Медицинский полис должен содержать 16 цифр";
            }
            else
            {
                MedicalPolicyError = null;
            }
        }

        private void ValidateMedicalFacility() => MedicalFacilityError = string.IsNullOrWhiteSpace(MedicalFacility) ? "Медицинское учреждение обязательно" : null;

        private void ValidateWorkplace() => WorkplaceError = string.IsNullOrWhiteSpace(Workplace) ? "Место работы обязательно" : null;

        private void ValidateOwnershipForm() => OwnershipFormError = string.IsNullOrWhiteSpace(OwnershipForm) ? "Форма собственности обязательна" : null;

        private void ValidateOkved()
        {
            if (string.IsNullOrWhiteSpace(Okved))
            {
                OkvedError = "ОКВЭД обязателен";
            }
            else if (Okved.Replace(".", "").Length < 4)
            {
                OkvedError = "ОКВЭД должен содержать минимум 4 цифры";
            }
            else
            {
                OkvedError = null;
            }
        }

        private void ValidateWorkExperience()
        {
            if (string.IsNullOrWhiteSpace(WorkExperience))
            {
                WorkExperienceError = "Стаж работы обязателен";
            }
            else if (!int.TryParse(WorkExperience.Replace(" лет", ""), out int years) || years < 0 || years > 80)
            {
                WorkExperienceError = "Стаж должен быть числом от 0 до 80";
            }
            else
            {
                WorkExperienceError = null;
            }
        }

        private void ValidateSelectedOrderClauses() => SelectedOrderClausesError = (SelectedOrderClauses == null || !SelectedOrderClauses.Any()) ? "Не выбраны пункты вредности" : null;
    }
}