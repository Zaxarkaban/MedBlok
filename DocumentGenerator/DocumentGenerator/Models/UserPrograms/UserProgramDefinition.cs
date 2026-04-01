using System;
using System.Collections.Generic;

namespace DocumentGenerator.Models.UserPrograms
{
    public sealed class UserProgramDefinition
    {
        public int SchemaVersion { get; set; } = 1;

        public string Id { get; set; } = Guid.NewGuid().ToString("N");

        public string Name { get; set; } = "Новая программа";

        /// <summary>
        /// File name inside program folder (usually template.pdf).
        /// </summary>
        public string TemplateFileName { get; set; } = "template.pdf";

        public List<UserProgramFieldDefinition> Fields { get; set; } = new();
    }
}

