using Microsoft.EntityFrameworkCore;

namespace DocumentGenerator.Models
{
    public class Record
    {
        public string FullName { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public string DateOfBirth { get; set; } = string.Empty;
        public int Age { get; set; }
        public string Gender { get; set; } = string.Empty;
        public string OrderClauses { get; set; } = string.Empty;
        public string Snils { get; set; } = string.Empty;
        public string MedicalPolicy { get; set; } = string.Empty;
        public string PassportSeries { get; set; } = string.Empty;
        public string PassportNumber { get; set; } = string.Empty;
        public string PassportIssueDate { get; set; } = string.Empty;
        public string PassportIssuedBy { get; set; } = string.Empty;
    }
}