using Avalonia.Threading;
using ReactiveUI;
using System.Collections.ObjectModel;
using DocumentGenerator.Services;
using System.Threading.Tasks;
using OfficeOpenXml;
using System.Collections.Generic;
using System.Linq;

namespace DocumentGenerator.ViewModels
{
    public class ExcelDataViewModel : ReactiveObject
    {
        private ObservableCollection<Dictionary<string, string>> _peopleData = new ObservableCollection<Dictionary<string, string>>();
        private readonly DocumentService _documentService;

        public ObservableCollection<Dictionary<string, string>> PeopleData
        {
            get => _peopleData;
            set => this.RaiseAndSetIfChanged(ref _peopleData, value);
        }

        public ExcelDataViewModel(DocumentService documentService)
        {
            _documentService = documentService;
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        public async Task LoadFromExcel(string filePath)
        {
            var peopleList = await Task.Run(() =>
            {
                using var package = new ExcelPackage(new System.IO.FileInfo(filePath));
                var worksheet = package.Workbook.Worksheets[0];
                int rowCount = worksheet.Dimension.Rows;

                var result = new List<Dictionary<string, string>>();
                for (int row = 2; row <= rowCount; row++)
                {
                    var personData = new Dictionary<string, string>
                    {
                        { "FullName", worksheet.Cells[row, 1].Text },
                        { "Position", worksheet.Cells[row, 2].Text },
                        { "DateOfBirth", worksheet.Cells[row, 3].Text },
                        { "Gender", worksheet.Cells[row, 4].Text },
                        { "Snils", worksheet.Cells[row, 5].Text },
                        { "PassportSeries", worksheet.Cells[row, 6].Text },
                        { "PassportNumber", worksheet.Cells[row, 7].Text },
                        { "PassportIssueDate", worksheet.Cells[row, 8].Text },
                        { "PassportIssuedBy", worksheet.Cells[row, 9].Text },
                        { "MedicalPolicy", worksheet.Cells[row, 10].Text },
                        { "Address", worksheet.Cells[row, 11].Text },
                        { "Phone", worksheet.Cells[row, 12].Text },
                        { "MedicalOrganization", worksheet.Cells[row, 13].Text },
                        { "MedicalFacility", worksheet.Cells[row, 14].Text },
                        { "Workplace", worksheet.Cells[row, 15].Text },
                        { "OwnershipForm", worksheet.Cells[row, 16].Text },
                        { "Okved", worksheet.Cells[row, 17].Text },
                        { "WorkExperience", worksheet.Cells[row, 18].Text },
                        { "OrderClause", worksheet.Cells[row, 19].Text }
                    };

                    int age = CalculateAge(personData["DateOfBirth"]);
                    bool isFemale = personData["Gender"] == "Женский";
                    bool isOver40 = age > 40;
                    var orderClauses = personData["OrderClause"]?.Split(',', System.StringSplitOptions.RemoveEmptyEntries).ToList() ?? new List<string>();
                    var doctorsList = _documentService.GenerateDoctorsList(orderClauses, isOver40, isFemale);
                    personData["DoctorsList"] = string.Join(", ", doctorsList);

                    result.Add(personData);
                }
                return result;
            });

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                PeopleData = new ObservableCollection<Dictionary<string, string>>(peopleList);
            });
        }

        private int CalculateAge(string dateOfBirth)
        {
            if (string.IsNullOrEmpty(dateOfBirth)) return 0;

            if (!System.DateTime.TryParseExact(dateOfBirth, "dd.MM.yyyy", null, System.Globalization.DateTimeStyles.None, out var dob))
                return 0;

            var today = System.DateTime.Today;
            int age = today.Year - dob.Year;
            if (dob.Date > today.AddYears(-age)) age--;
            return age;
        }
    }
}