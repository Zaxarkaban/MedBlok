using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;

namespace DocumentGenerator.Models.UserPrograms
{
    public enum UserProgramFieldType
    {
        Text,
        Number,
        Date,
        Combo,
        Checkbox,
        MultiSelect
    }

    public sealed class UserProgramFieldDefinition : ObservableObject
    {
        private string _key = "";
        public string Key
        {
            get => _key;
            set => SetProperty(ref _key, value);
        }

        private UserProgramFieldType _type = UserProgramFieldType.Text;
        public UserProgramFieldType Type
        {
            get => _type;
            set => SetProperty(ref _type, value);
        }

        /// <summary>
        /// Internal display name for the block (for the constructor UI).
        /// </summary>
        private string _blockName = "";
        public string BlockName
        {
            get => _blockName;
            set => SetProperty(ref _blockName, value);
        }

        private string _label = "";
        public string Label
        {
            get => _label;
            set => SetProperty(ref _label, value);
        }

        /// <summary>
        /// Must strictly match AcroForm field name inside template PDF.
        /// </summary>
        private string _pdfFieldName = "";
        public string PdfFieldName
        {
            get => _pdfFieldName;
            set => SetProperty(ref _pdfFieldName, value);
        }

        private bool _required;
        public bool Required
        {
            get => _required;
            set => SetProperty(ref _required, value);
        }

        private List<string>? _options;
        public List<string>? Options
        {
            get => _options;
            set => SetProperty(ref _options, value);
        }

        private List<UserProgramValidationRule> _validation = new();
        public List<UserProgramValidationRule> Validation
        {
            get => _validation;
            set => SetProperty(ref _validation, value);
        }
    }
}

