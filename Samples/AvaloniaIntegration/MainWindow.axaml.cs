using FFmpegBindings.Linked;
using Avalonia.Controls;


namespace AvaloniaIntegration;



public partial class MainWindow : Window
{
    private const string TestVideoUrl = "http://commondatastorage.googleapis.com/gtv-videos-bucket/sample/BigBuckBunny.mp4";
    
    private VideoPlayer _player;
    
    public MainWindow()
    {
        InitializeComponent();
        FFmpegLinked.Init();

        _player = new VideoPlayer(TestVideoUrl ,Frame);
    }
}