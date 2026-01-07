using Avalonia.Controls;

namespace AvaloniaIntegration;

using FFmpegBindings.Abstractions;
using FFmpegBindings.Linked;

using FFmpegWrapper.Codecs.Decoding;
using FFmpegWrapper.Core;
using FFmpegWrapper.Media;
using FFmpegWrapper.Media.Formats;
using FFmpegWrapper.Media.Frames;
using FFmpegWrapper.Media.Packets;

public partial class MainWindow : Window
{
    private MediaDemuxer _demuxer;
    private VideoDecoder _decoder;
    private VideoFrame _currentFrame;
    private MediaPacket _packet;
    
    public MainWindow()
    {
        InitializeComponent();
        
        FFmpegLinked.Init();
        _demuxer = new MediaDemuxer("http://commondatastorage.googleapis.com/gtv-videos-bucket/sample/BigBuckBunny.mp4"u8);
        _packet = new MediaPacket();
        
        if (_demuxer.TryFindBestStream(AVMediaType.AVMEDIA_TYPE_VIDEO, out var stream)) {
            _decoder = (VideoDecoder)_demuxer.CreateStreamDecoder(stream.Handle);
            
            _currentFrame = new VideoFrame(_decoder.FrameFormat);
            _demuxer.Seek(TimeSpan.FromSeconds(10));

            while (_demuxer.Read(_packet.Handle)) {
                if (_packet.StreamIndex != stream.Index) continue; //Ignore packets from other streams

                if (_decoder.TrySendPacket(_packet.Handle) is not LavResult.Success) {
                    _decoder.Flush();
                    continue;
                }
                
                if (!_decoder.ReceiveFrame(_currentFrame.Handle)) {
                    continue;
                }

                Frame.SetFrame(_currentFrame);
                break;
            }

        }

    }
    
    
}