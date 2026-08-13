using System;
using System.IO;
using System.Text.Json;

namespace DocumentGenerator.Services
{
    public interface IExportPathMemory
    {
        string? LastExcelFilePath { get; }
        string? LastExportFolderPath { get; }
        void RememberExcelFile(string filePath);
        void RememberExportFolder(string folderPath);
    }

    /// <summary>
    /// Запоминает последние пути Excel и папки выгрузки PDF.
    /// </summary>
    public sealed class ExportPathMemory : IExportPathMemory
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        private readonly string _settingsPath;
        private string? _lastExcelFile;
        private string? _lastExportFolder;

        public ExportPathMemory()
        {
            var root = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "DocumentGenerator");
            _settingsPath = Path.Combine(root, "export-paths.json");
            Load();
        }

        public string? LastExcelFilePath => _lastExcelFile;
        public string? LastExportFolderPath => _lastExportFolder;

        public void RememberExcelFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                return;

            _lastExcelFile = filePath;
            Save();
        }

        public void RememberExportFolder(string folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
                return;

            _lastExportFolder = folderPath;
            Save();
        }

        private void Load()
        {
            try
            {
                if (!File.Exists(_settingsPath))
                    return;

                var dto = JsonSerializer.Deserialize<ExportPathsDto>(File.ReadAllText(_settingsPath), JsonOptions);
                if (dto == null)
                    return;

                if (!string.IsNullOrWhiteSpace(dto.LastExcelFile) && File.Exists(dto.LastExcelFile))
                    _lastExcelFile = dto.LastExcelFile;

                if (!string.IsNullOrWhiteSpace(dto.LastExportFolder) && Directory.Exists(dto.LastExportFolder))
                    _lastExportFolder = dto.LastExportFolder;
            }
            catch
            {
                // ignore corrupt settings
            }
        }

        private void Save()
        {
            try
            {
                var dir = Path.GetDirectoryName(_settingsPath);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);

                var dto = new ExportPathsDto
                {
                    LastExcelFile = _lastExcelFile,
                    LastExportFolder = _lastExportFolder
                };
                File.WriteAllText(_settingsPath, JsonSerializer.Serialize(dto, JsonOptions));
            }
            catch
            {
                // ignore persistence failure
            }
        }

        private sealed class ExportPathsDto
        {
            public string? LastExcelFile { get; set; }
            public string? LastExportFolder { get; set; }
        }
    }
}
