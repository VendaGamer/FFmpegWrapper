namespace AvaloniaIntegration;

using System.Buffers;
using System.Text;

using FFmpegWrapper.Media;
using FFmpegWrapper.Media.Frames;
using FFmpegWrapper.Media.Packets;

public sealed class VideoPlayer : IDisposable
{
    private Queue<MediaPacket> _packetQueue;
    private Queue<VideoFrame> _frameQueue;
    private Queue<AudioFrame> _audioQueue;
    
    private MediaDemuxer _demuxer;
    private Thread _demuxerThread;
    
    public VideoPlayer(string filePath, VideoFrameControl frameRenderVisual)
    {
        var maxLenght = Encoding.UTF8.GetMaxByteCount(filePath.Length);

        if (maxLenght > 1024) {
            var buffer = ArrayPool<byte>.Shared.Rent(maxLenght);
            Encoding.UTF8.GetBytes(filePath, buffer);
            _demuxer = new MediaDemuxer(buffer);
        } else {
            Span<byte> buffer = stackalloc byte[maxLenght];
            Encoding.UTF8.GetBytes(filePath, buffer);
            _demuxer = new MediaDemuxer(buffer);
        }
        
        _packetQueue = new Queue<MediaPacket>(90);
        _frameQueue = new Queue<VideoFrame>(6);
        _audioQueue = new Queue<AudioFrame>(9);

        _demuxerThread = new Thread();
    }

    
    /// <returns>true if seeked to the exact position or false if position had to be capped</returns>
    public bool SeekTo(TimeSpan position)
    {
        
    }

    public void Play()
    {
        
    }

    public void Pause()
    {
        
    }

    public void Stop()
    {
        
    }
    
    private void DecodeLoop()
    {
       
    }

    public void Dispose()
    {
        
    }
}