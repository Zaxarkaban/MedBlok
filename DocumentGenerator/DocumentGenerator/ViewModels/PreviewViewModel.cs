using DocumentGenerator.Data.Entities;
using DocumentGenerator.ViewModels;
using System.Collections.ObjectModel;

namespace DocumentGenerator.ViewModels // Добавляем пространство имён
{
    public class PreviewViewModel : ViewModelBase
    {
        private ObservableCollection<OrderClause> _selectedOrderClauses = new ObservableCollection<OrderClause>();
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

        public ObservableCollection<OrderClause> SelectedOrderClauses
        {
            get => _selectedOrderClauses;
            set => SetProperty(ref _selectedOrderClauses, value);
        }

        public string FullName
        {
            get => _fullName;
            set => SetProperty(ref _fullName, value);
        }

        public string Position
        {
            get => _position;
            set => SetProperty(ref _position, value);
        }

        public string DateOfBirth
        {
            get => _dateOfBirth;
            set => SetProperty(ref _dateOfBirth, value);
        }

        public string Gender
        {
            get => _gender;
            set => SetProperty(ref _gender, value);
        }

        public string Snils
        {
            get => _snils;
            set => SetProperty(ref _snils, value);
        }

        public string PassportSeries
        {
            get => _passportSeries;
            set => SetProperty(ref _passportSeries, value);
        }

        public string PassportNumber
        {
            get => _passportNumber;
            set => SetProperty(ref _passportNumber, value);
        }

        public string PassportIssueDate
        {
            get => _passportIssueDate;
            set => SetProperty(ref _passportIssueDate, value);
        }

        public string PassportIssuedBy
        {
            get => _passportIssuedBy;
            set => SetProperty(ref _passportIssuedBy, value);
        }

        public string Address
        {
            get => _address;
            set => SetProperty(ref _address, value);
        }

        public string Phone
        {
            get => _phone;
            set => SetProperty(ref _phone, value);
        }

        public string MedicalOrganization
        {
            get => _medicalOrganization;
            set => SetProperty(ref _medicalOrganization, value);
        }

        public string MedicalPolicy
        {
            get => _medicalPolicy;
            set => SetProperty(ref _medicalPolicy, value);
        }

        public string MedicalFacility
        {
            get => _medicalFacility;
            set => SetProperty(ref _medicalFacility, value);
        }

        public string Workplace
        {
            get => _workplace;
            set => SetProperty(ref _workplace, value);
        }

        public string OwnershipForm
        {
            get => _ownershipForm;
            set => SetProperty(ref _ownershipForm, value);
        }

        public string Okved
        {
            get => _okved;
            set => SetProperty(ref _okved, value);
        }

        public string WorkExperience
        {
            get => _workExperience;
            set => SetProperty(ref _workExperience, value);
        }

        public PreviewViewModel(MainWindowViewModel mainViewModel)
        {
            // Копируем данные из MainWindowViewModel
            FullName = mainViewModel.FullName;
            Position = mainViewModel.Position;
            DateOfBirth = mainViewModel.DateOfBirth;
            Gender = mainViewModel.Gender;
            Snils = mainViewModel.Snils;
            PassportSeries = mainViewModel.PassportSeries;
            PassportNumber = mainViewModel.PassportNumber;
            PassportIssueDate = mainViewModel.PassportIssueDate;
            PassportIssuedBy = mainViewModel.PassportIssuedBy;
            Address = mainViewModel.Address;
            Phone = mainViewModel.Phone;
            MedicalOrganization = mainViewModel.MedicalOrganization;
            MedicalPolicy = mainViewModel.MedicalPolicy;
            MedicalFacility = mainViewModel.MedicalFacility;
            Workplace = mainViewModel.Workplace;
            OwnershipForm = mainViewModel.OwnershipForm;
            Okved = mainViewModel.Okved;
            WorkExperience = mainViewModel.WorkExperience;
            SelectedOrderClauses = mainViewModel.SelectedOrderClauses ?? new ObservableCollection<OrderClause>();
        }
    }
}