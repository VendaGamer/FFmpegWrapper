using Avalonia.Controls;

namespace AvaloniaIntegration;

using FFmpegBindings.Abstractions;
using FFmpegBindings.Linked;

using FFmpegWrapper.Codecs.Decoding;
using FFmpegWrapper.Media;

public partial class MainWindow : Window
{
    private MediaDemuxer _demuxer;
    private VideoDecoder _decoder;
    public MainWindow()
    {
        InitializeComponent();
        
        FFmpegLinked.Init();

        _demuxer = new MediaDemuxer("http://commondatastorage.googleapis.com/gtv-videos-bucket/sample/BigBuckBunny.mp4"u8);
        
        if (_demuxer.TryFindBestStream(AVMediaType.AVMEDIA_TYPE_VIDEO, out var stream)) {
            _decoder = (VideoDecoder)_demuxer.CreateStreamDecoder(stream.Handle);
        }
        

    }
    
    
}