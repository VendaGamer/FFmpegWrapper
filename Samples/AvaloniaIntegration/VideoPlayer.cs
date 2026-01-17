using FFmpegBindings.Abstractions;
using FFmpegWrapper.Codecs.Decoding;
using FFmpegWrapper.Core;
using FFmpegWrapper.Media.Streams;

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
    private VideoDecoder _decoder;
    private MediaStream _videoStream;
    private Thread _demuxerThread;
    private Mutex _demuxingMutex;
    
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
            if (_demuxer.TryFindBestStream(AVMediaType.AVMEDIA_TYPE_VIDEO, out _videoStream))
            {
                _decoder = (VideoDecoder)_demuxer.CreateStreamDecoder(_videoStream.Handle);
            }
        }
        
        _packetQueue = new Queue<MediaPacket>(90);
        _frameQueue = new Queue<VideoFrame>(6);
        _audioQueue = new Queue<AudioFrame>(9);

        _demuxerThread = new Thread(DecodeLoop);
        _demuxingMutex = new Mutex();
        _demuxerThread.Start();
    }

    
    private void DecodeLoop()
    {
        while (true)
        {
            MediaPacket packet;
            
            if (_packetQueue.Count < _packetQueue.Capacity)
            {
                packet = new MediaPacket();
                goto Read;
            }

            _demuxingMutex.WaitOne();
            packet = _packetQueue.Dequeue();
            
            Read:
            var result = _demuxer.Read(packet);

            if (result < 0)
            {
                switch (result)
                {
                    case LavResult.EndOfFile:
                        _demuxingMutex.WaitOne();
                        break;
                    case LavResult.TryAgain:
                        goto Read;
                    default:
                        throw new Exception($"unexpected result {result}");
                }
            }
            
            _packetQueue.Enqueue(packet);
        }
    }
    
    
    
    /// <returns>true if seeked to the exact position or false if position had to be capped</returns>
    public bool SeekTo(TimeSpan position)
    {
        return false;
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

    public void Dispose()
    {
        
    }
}