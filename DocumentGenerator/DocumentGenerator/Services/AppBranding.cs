using System;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using DocumentGenerator.Models;

namespace DocumentGenerator.Services
{
    public static class AppBranding
    {
        public const string AppName = "МедДок Генератор";

        private static WindowIcon? _windowIcon;
        private static readonly Uri LogoUri = new("avares://DocumentGenerator/Assets/brand-logo.png");
        private static readonly Uri IconUri = new("avares://DocumentGenerator/Assets/app-icon.png");

        public static WindowIcon WindowIcon => _windowIcon ??= LoadWindowIcon();

        public static void ApplyWindowBranding(Window window)
        {
            window.Icon = WindowIcon;
        }

        public static void ApplyMenuLogo(Image logoImage, AppThemeMode _)
        {
            logoImage.Source = new Bitmap(AssetLoader.Open(LogoUri));
        }

        private static WindowIcon LoadWindowIcon()
        {
            return new WindowIcon(AssetLoader.Open(IconUri));
        }
    }
}
