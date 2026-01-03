namespace GLPlayer;

using System.Diagnostics;
using FFmpegWrapper.Codecs.Decoding;
using FFmpegWrapper.Media;
using FFmpegWrapper.Media.Frames;
using FFmpegWrapper.Media.Packets;
using FFmpegWrapper.Media.Streams;

public abstract class StreamRenderer(MediaDemuxer demuxer, MediaStream stream) : IDisposable
{
    public MediaStream Stream { get; } = stream;
    protected MediaDecoder _decoder = demuxer.CreateStreamDecoder(stream.Handle, open: false);

    private Queue<MediaPacket> _packetQueue = new();
    public PlayerClock Clock { get; } = new();

    public bool EnqueuePacket(MediaPacket packet)
    {
        if (_packetQueue.Count < 128) {
            _packetQueue.Enqueue(packet);
            return true;
        }
        return false;
    }

    protected bool ReceiveFrame(MediaFrame frame)
    {
        while (true) {
            if (_decoder.ReceiveFrame(frame)) {
                return true;
            }
            if (_packetQueue.TryDequeue(out var packet)) {
                _decoder.SendPacket(packet);
                packet.Dispose();
            } else {
                return false;
            }
        }
    }

    /// <summary> Flush decoder and buffered frames. </summary>
    public virtual void Flush()
    {
        _decoder.Flush();
        _packetQueue.Clear();
    }

    public virtual void Dispose()
    {
        _decoder.Dispose();
    }
}
public class PlayerClock
{
    public TimeSpan FramePts;         //PTS of the current frame
    public TimeSpan FrameDisplayTime; //Clock time at which the current frame was displayed

    public TimeSpan GetFrameTime()
    {
        return FramePts + (GetCurrentTime() - FrameDisplayTime);
    }
    public void SetFrameTime(TimeSpan pts)
    {
        FramePts = pts;
        FrameDisplayTime = GetCurrentTime();
    }

    static readonly long s_GlobalStartTime = Stopwatch.GetTimestamp();
    public static TimeSpan GetCurrentTime() => Stopwatch.GetElapsedTime(s_GlobalStartTime);

}