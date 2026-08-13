using System;
using System.Collections.Generic;
using System.IO;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Layout;
using iText.Layout.Properties;
using iText.Layout.Renderer;

namespace DocumentGenerator.Services
{
    /// <summary>
    /// Рисует список исследований в пустую область 1-й страницы шаблона (ландшафт).
    /// Зона — весь пробел между «12. Объём исследований» и «13. Заключения…»,
    /// левее вертикали таблицы врачей (номера 4–8). Шрифт уменьшается, пока список не влезет.
    /// </summary>
    public static class TestsListPdfService
    {
        // Page ≈ 842×595 (PDF bottom-left).
        // «12.» ≈ y571.5–586.8; «13.» ≈ y261.7–277.2; вертикаль таблицы ≈ x435.2
        private const float AreaX = 24f;
        private const float AreaY = 280f;
        private const float AreaWidth = 406f;
        private const float AreaHeight = 286f;

        /// <summary>Внутренний отступ текста от границ области (не залезать на подписи бланка).</summary>
        private const float ContentPadding = 5f;

        private const float InitialFontSize = 9f;
        private const float MinFontSize = 3.5f;
        private const float ShrinkStep = 0.1f;
        private const float LeadingMultiplier = 1.1f;

        public static void DrawOnFirstPage(PdfDocument pdfDocument, IReadOnlyList<string> tests, PdfFont font)
        {
            if (pdfDocument == null || tests == null || tests.Count == 0)
                return;

            if (pdfDocument.GetNumberOfPages() < 1)
                pdfDocument.AddNewPage();

            var page = pdfDocument.GetPage(1);
            var clipArea = new Rectangle(AreaX, AreaY, AreaWidth, AreaHeight);
            var contentArea = new Rectangle(
                AreaX + ContentPadding,
                AreaY + ContentPadding,
                Math.Max(1f, AreaWidth - 2f * ContentPadding),
                Math.Max(1f, AreaHeight - 2f * ContentPadding));

            float fontSize = FindFittingFontSize(tests, font, contentArea);
            var paragraph = BuildParagraph(tests, font, fontSize);

            var pdfCanvas = new PdfCanvas(page);
            pdfCanvas.SaveState();
            pdfCanvas.Rectangle(clipArea);
            pdfCanvas.Clip();
            pdfCanvas.EndPath();

            var canvas = new Canvas(pdfCanvas, contentArea);
            canvas.Add(paragraph);
            canvas.Close();

            pdfCanvas.RestoreState();
        }

        private static float FindFittingFontSize(IReadOnlyList<string> tests, PdfFont font, Rectangle area)
        {
            for (float fontSize = InitialFontSize; fontSize >= MinFontSize; fontSize -= ShrinkStep)
            {
                float size = (float)Math.Round(fontSize, 2);
                if (size < MinFontSize)
                    size = MinFontSize;

                if (Fits(BuildParagraph(tests, font, size), area))
                    return size;
            }

            return MinFontSize;
        }

        private static bool Fits(Paragraph paragraph, Rectangle area)
        {
            using var ms = new MemoryStream();
            using var writer = new PdfWriter(ms);
            using var measurePdf = new PdfDocument(writer);
            measurePdf.AddNewPage(new PageSize(area.GetWidth() + 72, area.GetHeight() + 72));

            using var document = new Document(measurePdf);
            var renderer = paragraph.CreateRendererSubTree().SetParent(document.GetRenderer());
            var result = renderer.Layout(new LayoutContext(new LayoutArea(1, new Rectangle(area.GetWidth(), area.GetHeight()))));
            return result.GetStatus() == LayoutResult.FULL;
        }

        private static Paragraph BuildParagraph(IReadOnlyList<string> tests, PdfFont font, float fontSize)
        {
            var paragraph = new Paragraph()
                .SetFont(font)
                .SetFontSize(fontSize)
                .SetMultipliedLeading(LeadingMultiplier)
                .SetMargin(0)
                .SetPadding(0)
                .SetTextAlignment(TextAlignment.LEFT);

            for (int i = 0; i < tests.Count; i++)
            {
                if (i > 0)
                    paragraph.Add("\n");
                paragraph.Add($"{i + 1}. {tests[i]}");
            }

            return paragraph;
        }
    }
}
