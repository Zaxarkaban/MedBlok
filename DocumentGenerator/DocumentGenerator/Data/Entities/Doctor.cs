namespace DocumentGenerator.Data.Entities
{
    public class Doctor
    {
        public int Id { get; set; }
        public string DoctorName { get; set; } = string.Empty;
        public int ClauseId { get; set; }
        public OrderClause OrderClause { get; set; } = null!;
    }
}