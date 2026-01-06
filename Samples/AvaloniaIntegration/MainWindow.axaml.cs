using Avalonia.Controls;

namespace AvaloniaIntegration;

using Avalonia.Media.Imaging;

using SkiaSharp;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        SKBitmap bitmap = new SKBitmap(100, 100);
        bitmap.InstallPixels(new SKImageInfo(100, 100, SKColorType.Rgba8888, SKAlphaType.Premul));
        InitializeComponent();
    }
}