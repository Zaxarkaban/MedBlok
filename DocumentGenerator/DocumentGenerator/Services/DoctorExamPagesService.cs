using System;
using System.Collections.Generic;
using System.IO;
using iText.Forms;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;
using iText.Kernel.Pdf.Xobject;

namespace DocumentGenerator.Services
{
    /// <summary>
    /// Бланки осмотров врачей: ландшафт A4, 2 бланка влево/вправо.
    /// В PDF-штампах контент лежит в левой половине страницы — её и растягиваем на пол-листа.
    /// Заполняется только бланк профпатолога (все известные поля); остальные — пустые.
    /// </summary>
    public static class DoctorExamPagesService
    {
        private const string PathologistFile = "pathologist.pdf";

        // Рабочая область штампа (контент ~24..439 по X, правая половина листа пустая)
        private const float StampContentX = 20f;
        private const float StampContentWidth = 430f;

        // Отступы от краёв листа и зазор между левым/правым бланком
        private const float PageMargin = 16f;
        private const float PageMarginLeft = 32f; // в 2 раза больше от левого края
        private const float HalfGap = 8f;

        private static readonly Dictionary<string, string> FileByDoctor =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["Терапевт"] = "therapist.pdf",
                ["Дерматовенеролог"] = "dermatologist.pdf",
                ["Психиатр"] = "psychiatrist.pdf",
                ["Нарколог"] = "narcologist.pdf",
                ["Невролог"] = "neurologist.pdf",
                ["Офтальмолог"] = "ophthalmologist.pdf",
                ["Оториноларинголог"] = "ent.pdf",
                ["Хирург"] = "surgeon.pdf",
                ["Стоматолог"] = "dentist.pdf",
                ["Акушер-гинеколог"] = "gynecologist.pdf",
                ["Профпатолог"] = PathologistFile,
            };

        public static void AppendDoctorExamPages(
            PdfDocument pdfDocument,
            IReadOnlyList<string> doctors,
            IReadOnlyDictionary<string, string>? patientData = null)
        {
            if (doctors == null || doctors.Count == 0)
                return;

            var files = ResolveStampFiles(doctors);
            if (files.Count == 0)
                return;

            string formsDir = GetDoctorFormsDirectory();
            var pageSize = PageSize.A4.Rotate();

            for (int i = 0; i < files.Count; i += 2)
            {
                var page = pdfDocument.AddNewPage(pageSize);
                float pageW = page.GetPageSize().GetWidth();
                float pageH = page.GetPageSize().GetHeight();
                float halfW = pageW / 2f;

                PlaceStamp(pdfDocument, page, formsDir, files[i], leftHalf: true, halfW, pageH, patientData);

                if (i + 1 < files.Count)
                    PlaceStamp(pdfDocument, page, formsDir, files[i + 1], leftHalf: false, halfW, pageH, patientData);

                DrawVerticalSeparator(page, halfW, pageH);
            }
        }

        private static void PlaceStamp(
            PdfDocument dest,
            PdfPage destPage,
            string formsDir,
            string fileName,
            bool leftHalf,
            float halfW,
            float pageH,
            IReadOnlyDictionary<string, string>? patientData)
        {
            string stampPath = System.IO.Path.Combine(formsDir, fileName);
            bool fillPathologist = fileName.Equals(PathologistFile, StringComparison.OrdinalIgnoreCase)
                                   && patientData != null
                                   && patientData.Count > 0;

            PdfFormXObject xObject;
            float stampH;

            if (fillPathologist)
            {
                using var filledMs = CreateFilledStampStream(stampPath, patientData!);
                using var filledReader = new PdfReader(filledMs);
                using var filledDoc = new PdfDocument(filledReader);
                var srcPage = filledDoc.GetFirstPage();
                stampH = srcPage.GetPageSize().GetHeight();
                xObject = srcPage.CopyAsFormXObject(dest);
            }
            else
            {
                using var reader = new PdfReader(stampPath);
                using var src = new PdfDocument(reader);
                var srcPage = src.GetFirstPage();
                stampH = srcPage.GetPageSize().GetHeight();
                xObject = srcPage.CopyAsFormXObject(dest);
            }

            float slotX = leftHalf ? PageMarginLeft : halfW + HalfGap;
            float slotW = leftHalf
                ? halfW - PageMarginLeft - HalfGap
                : halfW - PageMargin - HalfGap;
            float slotY = PageMargin;
            float slotH = pageH - 2f * PageMargin;

            float scaleX = slotW / StampContentWidth;
            float scaleY = slotH / stampH;

            var canvas = new PdfCanvas(destPage);
            canvas.SaveState();
            canvas.Rectangle(slotX, slotY, slotW, slotH);
            canvas.Clip();
            canvas.EndPath();

            canvas.ConcatMatrix(
                scaleX, 0, 0, scaleY,
                slotX - StampContentX * scaleX,
                slotY);
            canvas.AddXObjectAt(xObject, 0, 0);
            canvas.RestoreState();
        }

        /// <summary>
        /// Заполняет только поля бланка, для которых есть непустые данные, и flatten.
        /// </summary>
        private static MemoryStream CreateFilledStampStream(
            string stampPath,
            IReadOnlyDictionary<string, string> patientData)
        {
            var ms = new MemoryStream();
            using (var writer = new PdfWriter(ms))
            {
                writer.SetCloseStream(false);
                using var reader = new PdfReader(stampPath);
                using var doc = new PdfDocument(reader, writer);
                var localFont = PdfFontHelper.CreateTimesFont();
                var form = PdfAcroForm.GetAcroForm(doc, true);
                if (form != null)
                {
                    var fields = form.GetAllFormFields();
                    foreach (var fieldName in fields.Keys)
                    {
                        if (!patientData.TryGetValue(fieldName, out var value))
                            continue;
                        if (string.IsNullOrWhiteSpace(value))
                            continue;

                        PdfFormFieldHelper.SetFieldValue(fields, fieldName, value.Trim(), localFont);
                    }

                    form.FlattenFields();
                }
            }

            ms.Position = 0;
            return ms;
        }

        private static void DrawVerticalSeparator(PdfPage page, float halfW, float pageH)
        {
            var canvas = new PdfCanvas(page);
            canvas.SaveState();
            canvas.SetLineDash(3, 3);
            canvas.SetLineWidth(0.5f);
            canvas.MoveTo(halfW, PageMargin);
            canvas.LineTo(halfW, pageH - PageMargin);
            canvas.Stroke();
            canvas.RestoreState();
        }

        private static List<string> ResolveStampFiles(IReadOnlyList<string> doctors)
        {
            var result = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var doctor in doctors)
            {
                if (!FileByDoctor.TryGetValue(doctor.Trim(), out var fileName))
                {
                    Console.WriteLine($"Бланк осмотра для врача «{doctor}» не найден — страница пропущена.");
                    continue;
                }

                if (!seen.Add(fileName))
                    continue;

                string path = System.IO.Path.Combine(GetDoctorFormsDirectory(), fileName);
                if (!File.Exists(path))
                {
                    Console.WriteLine($"Файл бланка не найден: {path}");
                    continue;
                }

                result.Add(fileName);
            }

            // Профпатолог всегда в конце (всем пациентам)
            if (result.Remove(PathologistFile))
                result.Add(PathologistFile);
            else
            {
                string path = System.IO.Path.Combine(GetDoctorFormsDirectory(), PathologistFile);
                if (File.Exists(path))
                    result.Add(PathologistFile);
            }

            return result;
        }

        private static string GetDoctorFormsDirectory()
        {
            return System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DoctorForms");
        }
    }
}
