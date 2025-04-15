using Avalonia.Controls;
using Avalonia.Interactivity;
using DocumentGenerator.ViewModels;

namespace DocumentGenerator
{
    public partial class MainWindow : Window
    {
        private MainWindowViewModel ViewModel => (MainWindowViewModel)DataContext;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainWindowViewModel();
        }

        private void FullName_TextChanged(object sender, TextChangedEventArgs e)
        {
            ViewModel.ValidateFullName();
        }

        private void Position_TextChanged(object sender, TextChangedEventArgs e)
        {
            ViewModel.ValidatePosition();
        }

        private void DateOfBirth_TextChanged(object sender, TextChangedEventArgs e)
        {
            ViewModel.ValidateDateOfBirth();
        }

        private void Gender_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ViewModel.ValidateGender();
        }

        private void OrderClause_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ViewModel.ValidateOrderClause();
        }

        private void Snils_TextChanged(object sender, TextChangedEventArgs e)
        {
            ViewModel.ValidateSnils();
        }

        private void PassportSeries_TextChanged(object sender, TextChangedEventArgs e)
        {
            ViewModel.ValidatePassportSeries();
        }

        private void PassportNumber_TextChanged(object sender, TextChangedEventArgs e)
        {
            ViewModel.ValidatePassportNumber();
        }

        private void PassportIssueDate_TextChanged(object sender, TextChangedEventArgs e)
        {
            ViewModel.ValidatePassportIssueDate();
        }

        private void PassportIssuedBy_TextChanged(object sender, TextChangedEventArgs e)
        {
            ViewModel.ValidatePassportIssuedBy();
        }

        private void MedicalPolicy_TextChanged(object sender, TextChangedEventArgs e)
        {
            ViewModel.ValidateMedicalPolicy();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.OnSave();
        }
    }
}