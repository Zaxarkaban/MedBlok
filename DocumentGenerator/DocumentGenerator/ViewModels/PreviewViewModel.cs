using ReactiveUI;
using System.Collections.ObjectModel;

namespace DocumentGenerator.ViewModels
{
    public class PreviewViewModel : ReactiveObject
    {
        private ObservableCollection<string> _selectedOrderClauses = new ObservableCollection<string>();
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

        public PreviewViewModel(DataEntryViewModel sourceViewModel = null)
        {
            if (sourceViewModel != null)
            {
                // Копируем данные из MainWindowViewModel
                FullName = sourceViewModel.FullName;
                Position = sourceViewModel.Position;
                DateOfBirth = sourceViewModel.DateOfBirth;
                Gender = sourceViewModel.Gender;
                Snils = sourceViewModel.Snils;
                PassportSeries = sourceViewModel.PassportSeries;
                PassportNumber = sourceViewModel.PassportNumber;
                PassportIssueDate = sourceViewModel.PassportIssueDate;
                PassportIssuedBy = sourceViewModel.PassportIssuedBy;
                MedicalPolicy = sourceViewModel.MedicalPolicy;
                Address = sourceViewModel.Address;
                Phone = sourceViewModel.Phone;
                MedicalOrganization = sourceViewModel.MedicalOrganization;
                MedicalFacility = sourceViewModel.MedicalFacility;
                Workplace = sourceViewModel.Workplace;
                OwnershipForm = sourceViewModel.OwnershipForm;
                Okved = sourceViewModel.Okved;
                WorkExperience = sourceViewModel.WorkExperience;
                SelectedOrderClauses = new ObservableCollection<string>(sourceViewModel.SelectedOrderClauses);
            }
        }

        public ObservableCollection<string> SelectedOrderClauses
        {
            get => _selectedOrderClauses;
            set => this.RaiseAndSetIfChanged(ref _selectedOrderClauses, value);
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
    }
}