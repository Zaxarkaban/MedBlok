namespace DocumentGenerator.Models.UserPrograms
{
    public enum UserProgramValidationType
    {
        Required,
        MaxLength,
        Regex,
        Range
    }

    public sealed class UserProgramValidationRule
    {
        public UserProgramValidationType Type { get; set; }

        public string? Message { get; set; }

        public int? MaxLength { get; set; }

        public string? RegexPattern { get; set; }

        public double? Min { get; set; }

        public double? Max { get; set; }
    }
}

