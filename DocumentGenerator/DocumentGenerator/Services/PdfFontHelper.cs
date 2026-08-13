using System;
using System.IO;
using iText.IO.Font;
using iText.Kernel.Font;

namespace DocumentGenerator.Services
{
    /// <summary>
    /// Загрузка Times New Roman для iText. PdfFont не кэшируется — один экземпляр нельзя
    /// переиспользовать между закрытыми PdfDocument.
    /// </summary>
    public static class PdfFontHelper
    {
        private static readonly object FontLock = new();
        private static string? _resolvedPath;
        private static byte[]? _fontBytes;
        private static bool _warmedUp;

        public static void Warmup()
        {
            if (_warmedUp)
                return;

            lock (FontLock)
            {
                if (_warmedUp)
                    return;

                try
                {
                    _ = CjkResourceLoader.GetRegistryNames();
                }
                catch
                {
                    // Первая инициализация CJK иногда бросает — повторная загрузка шрифта проходит.
                }

                try
                {
                    _ = CreateTimesFont();
                }
                catch
                {
                    // Шрифт может отсутствовать на этапе дизайна.
                }

                _warmedUp = true;
            }
        }

        public static string ResolveTimesFontPath()
        {
            if (_resolvedPath != null && File.Exists(_resolvedPath))
                return _resolvedPath;

            var fontsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Fonts");
            foreach (var name in new[] { "times.ttf", "TIMES.TTF", "Times.ttf" })
            {
                var candidate = Path.Combine(fontsDir, name);
                if (File.Exists(candidate))
                {
                    _resolvedPath = candidate;
                    return candidate;
                }
            }

            throw new FileNotFoundException(
                "Файл шрифта Times New Roman не найден. Ожидается Fonts/times.ttf в папке приложения.",
                Path.Combine(fontsDir, "times.ttf"));
        }

        public static PdfFont CreateTimesFont()
        {
            lock (FontLock)
            {
                var fontPath = ResolveTimesFontPath();
                PdfFontFactory.Register(fontPath);

                _fontBytes ??= File.ReadAllBytes(fontPath);
                FontProgram fontProgram = FontProgramFactory.CreateFont(_fontBytes);

                try
                {
                    return PdfFontFactory.CreateFont(
                        fontProgram,
                        PdfEncodings.IDENTITY_H,
                        PdfFontFactory.EmbeddingStrategy.FORCE_EMBEDDED);
                }
                catch (Exception ex) when (ex is NullReferenceException or TypeInitializationException)
                {
                    return PdfFontFactory.CreateFont(
                        fontProgram,
                        "Cp1251",
                        PdfFontFactory.EmbeddingStrategy.FORCE_EMBEDDED);
                }
            }
        }
    }
}
