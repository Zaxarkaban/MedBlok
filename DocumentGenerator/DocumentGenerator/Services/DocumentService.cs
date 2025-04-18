using System;
using System.Collections.Generic;
using System.Linq;
using iText.Kernel.Pdf;
using iText.Forms;
using DocumentGenerator.Models;
using iText.Kernel.Font;
using System.IO;
using iText.IO.Font;

namespace DocumentGenerator.Services
{
    public class DocumentService
    {
        public List<string> GenerateDoctorsList(List<string> selectedClauses)
        {
            // Создаём список обязательных врачей
            var mandatoryDoctors = new List<string> { "Терапевт", "Невролог", "Психиатр", "Нарколог" };

            // Получаем врачей из выбранных пунктов вредности
            var doctorsFromClauses = new List<string>();
            foreach (var clause in selectedClauses)
            {
                if (Dictionaries.OrderClauseDataMap.TryGetValue(clause, out var clauseData))
                {
                    doctorsFromClauses.AddRange(clauseData.Doctors);
                }
            }

            // Объединяем списки, исключаем дубликаты
            var allDoctors = mandatoryDoctors
                .Concat(doctorsFromClauses)
                .Distinct()
                .ToList();

            // Добавляем Профпатолога в конец
            allDoctors.Add("Профпатолог");

            return allDoctors;
        }

        public void FillPdfTemplate(Dictionary<string, string> userData, List<string> doctors, string templatePath = "template.pdf")
        {
            string outputPath = $"output_{DateTime.Now:yyyyMMddHHmmss}.pdf";

            try
            {
                using (var pdfReader = new PdfReader(templatePath))
                using (var pdfWriter = new PdfWriter(outputPath))
                using (var pdfDocument = new PdfDocument(pdfReader, pdfWriter))
                {
                    var form = PdfAcroForm.GetAcroForm(pdfDocument, true);
                    var fields = form.GetAllFormFields();

                    // Загружаем шрифт Times New Roman
                    string fontPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Fonts", "times.ttf");
                    if (!File.Exists(fontPath))
                    {
                        throw new FileNotFoundException("Times New Roman font file not found.", fontPath);
                    }

                    PdfFont font;
                    try
                    {
                        PdfFontFactory.Register(fontPath);
                        font = PdfFontFactory.CreateFont(fontPath, PdfEncodings.IDENTITY_H, PdfFontFactory.EmbeddingStrategy.FORCE_EMBEDDED);
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException($"Ошибка при загрузке шрифта: {ex.Message}", ex);
                    }

                    // Заполняем поля с данными пользователя
                    foreach (var data in userData)
                    {
                        if (fields.TryGetValue(data.Key, out var field))
                        {
                            field.SetValue(data.Value);
                            field.SetFontAndSize(font, 10);
                            field.RegenerateField(); // Обновляем поле для корректного отображения
                        }
                    }

                    // Заполняем врачей в поля Doctor_1 до Doctor_12
                    for (int i = 0; i < doctors.Count && i < 12; i++)
                    {
                        string fieldName = $"Doctor_{i + 1}";
                        if (fields.TryGetValue(fieldName, out var doctorField))
                        {
                            doctorField.SetValue(doctors[i]);
                            doctorField.SetFontAndSize(font, 10);
                            doctorField.RegenerateField(); // Обновляем поле для корректного отображения
                        }
                        else
                        {
                            Console.WriteLine($"Поле {fieldName} не найдено в шаблоне.");
                        }
                    }

                    if (doctors.Count > 12)
                    {
                        Console.WriteLine($"Внимание: В списке {doctors.Count} врачей, но шаблон поддерживает только 12. Лишние врачи проигнорированы.");
                    }

                    // Сохраняем изменения
                    form.FlattenFields();
                    pdfDocument.Close();
                }

                // Даём время на завершение записи файла перед открытием
                System.Threading.Thread.Sleep(500);

                // Открываем PDF-файл (для Windows)
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = outputPath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при заполнении PDF: {ex.Message}");
            }
        }
    }
}