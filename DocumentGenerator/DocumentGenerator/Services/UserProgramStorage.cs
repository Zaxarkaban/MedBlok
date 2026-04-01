using DocumentGenerator.Models.UserPrograms;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;

namespace DocumentGenerator.Services
{
    public sealed record UserProgramInfo(
        string Id,
        string Name,
        string FolderPath,
        string ProgramJsonPath,
        string TemplatePdfPath
    );

    public interface IUserProgramStorage
    {
        string RootFolderPath { get; }

        IReadOnlyList<UserProgramInfo> ListPrograms();

        UserProgramDefinition LoadDefinition(string programJsonPath);

        void SaveDefinition(string folderPath, UserProgramDefinition definition);

        string EnsureProgramFolder(string programId);

        string ExportToZip(UserProgramInfo program, string zipPath);

        UserProgramInfo ImportFromZip(string zipPath);

        void DeleteProgram(UserProgramInfo program);
    }

    public sealed class UserProgramStorage : IUserProgramStorage
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true
        };

        public string RootFolderPath { get; }

        public UserProgramStorage()
        {
            RootFolderPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "DocumentGenerator",
                "UserPrograms"
            );
        }

        public string EnsureProgramFolder(string programId)
        {
            Directory.CreateDirectory(RootFolderPath);
            var folder = Path.Combine(RootFolderPath, programId);
            Directory.CreateDirectory(folder);
            return folder;
        }

        public IReadOnlyList<UserProgramInfo> ListPrograms()
        {
            if (!Directory.Exists(RootFolderPath))
                return Array.Empty<UserProgramInfo>();

            var result = new List<UserProgramInfo>();
            foreach (var programJsonPath in Directory.EnumerateFiles(RootFolderPath, "program.json", SearchOption.AllDirectories))
            {
                try
                {
                    var folder = Path.GetDirectoryName(programJsonPath) ?? "";
                    var def = LoadDefinition(programJsonPath);
                    var templatePath = Path.Combine(folder, def.TemplateFileName ?? "template.pdf");
                    result.Add(new UserProgramInfo(def.Id, def.Name, folder, programJsonPath, templatePath));
                }
                catch
                {
                    // ignore broken entries for now
                }
            }

            return result
                .OrderBy(p => p.Name, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
        }

        public UserProgramDefinition LoadDefinition(string programJsonPath)
        {
            var json = File.ReadAllText(programJsonPath);
            var def = JsonSerializer.Deserialize<UserProgramDefinition>(json, JsonOptions);
            if (def == null)
                throw new InvalidOperationException("program.json is invalid or empty.");
            if (string.IsNullOrWhiteSpace(def.Id))
                throw new InvalidOperationException("program.json is missing Id.");
            if (string.IsNullOrWhiteSpace(def.Name))
                def.Name = def.Id;
            def.Fields ??= new List<UserProgramFieldDefinition>();
            return def;
        }

        public void SaveDefinition(string folderPath, UserProgramDefinition definition)
        {
            Directory.CreateDirectory(folderPath);
            var jsonPath = Path.Combine(folderPath, "program.json");
            var json = JsonSerializer.Serialize(definition, JsonOptions);
            File.WriteAllText(jsonPath, json);
        }

        public string ExportToZip(UserProgramInfo program, string zipPath)
        {
            if (File.Exists(zipPath))
                File.Delete(zipPath);

            ZipFile.CreateFromDirectory(program.FolderPath, zipPath, CompressionLevel.Optimal, includeBaseDirectory: false);
            return zipPath;
        }

        public UserProgramInfo ImportFromZip(string zipPath)
        {
            if (string.IsNullOrWhiteSpace(zipPath) || !File.Exists(zipPath))
                throw new FileNotFoundException("Zip file not found.", zipPath);

            // Extract into a temp folder first
            var tempFolder = Path.Combine(Path.GetTempPath(), $"dg_import_{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempFolder);
            ZipFile.ExtractToDirectory(zipPath, tempFolder);

            var programJson = Directory.EnumerateFiles(tempFolder, "program.json", SearchOption.AllDirectories).FirstOrDefault();
            if (programJson == null)
                throw new InvalidOperationException("Архив не содержит program.json.");

            var def = LoadDefinition(programJson);

            // Ensure unique id
            var newId = def.Id;
            var targetFolder = Path.Combine(RootFolderPath, newId);
            if (Directory.Exists(targetFolder))
            {
                newId = Guid.NewGuid().ToString("N");
                def.Id = newId;
            }

            var finalFolder = EnsureProgramFolder(newId);

            // Copy everything
            CopyDirectory(tempFolder, finalFolder);
            SaveDefinition(finalFolder, def); // ensure Id persisted

            var finalJson = Path.Combine(finalFolder, "program.json");
            var templatePath = Path.Combine(finalFolder, def.TemplateFileName ?? "template.pdf");
            return new UserProgramInfo(def.Id, def.Name, finalFolder, finalJson, templatePath);
        }

        public void DeleteProgram(UserProgramInfo program)
        {
            if (Directory.Exists(program.FolderPath))
            {
                Directory.Delete(program.FolderPath, recursive: true);
            }
        }

        private static void CopyDirectory(string sourceDir, string destDir)
        {
            Directory.CreateDirectory(destDir);

            foreach (var file in Directory.GetFiles(sourceDir))
            {
                var dest = Path.Combine(destDir, Path.GetFileName(file));
                File.Copy(file, dest, overwrite: true);
            }

            foreach (var dir in Directory.GetDirectories(sourceDir))
            {
                var destSub = Path.Combine(destDir, Path.GetFileName(dir));
                CopyDirectory(dir, destSub);
            }
        }
    }
}

