using FFmpegBindings.Linked;
using Avalonia.Controls;

namespace AvaloniaIntegration;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        FFmpegLinked.Init();
    }
}