namespace AvaloniaIntegration;

using Extensions;
using FFmpegBindings.Abstractions;
using FFmpegWrapper.Codecs.Decoding;
using FFmpegWrapper.Media;
using FFmpegWrapper.Media.Streams;
using SoundFlow.Enums;
using SoundFlow.Interfaces;
using SoundFlow.Metadata.Models;

public class DemuxerDataProvider : ISoundDataProvider
{
    
    public bool IsDisposed => _isDisposed;
    public int Length => FFmpeg.av_get_bytes_per_sample(_decoder.SampleFormat) * 
    public bool CanSeek => true;
    public SampleFormat SampleFormat => _decoder.SampleFormat.ToSoundFlow().sampleFormat;
    public int SampleRate => _decoder.SampleRate;

    public SoundFormatInfo FormatInfo => _formatInfo;
    public event EventHandler<EventArgs>? EndOfStreamReached;
    public event EventHandler<PositionChangedEventArgs>? PositionChanged;
    
    
    private bool _isDisposed;
    
    private MediaDemuxer _demuxer;
    private AudioDecoder _decoder;
    private MediaStream _audioStream;
    private SoundFormatInfo _formatInfo;
    private AudioQueue _audioQueue;
    
    
    public DemuxerDataProvider(MediaDemuxer mediaDemuxer)
    {
        _demuxer = mediaDemuxer;
        
        if(!_demuxer.TryFindBestStream(AVMediaType.AVMEDIA_TYPE_AUDIO, out _audioStream))
            throw new Exception("Could not find audio stream");
        
        _decoder = (AudioDecoder)_demuxer.CreateStreamDecoder(_audioStream.Handle);
        _audioQueue = new AudioQueue(_decoder.Format,  60);
    }
    
    public void Dispose()
    {
        _decoder.Dispose();
        _isDisposed = true;
    }

    public int ReadBytes(Span<float> buffer)
    {
        _audioQueue.
    }

    public void Seek(int offset)
    {
        
    }

    public int Position { get; }
}