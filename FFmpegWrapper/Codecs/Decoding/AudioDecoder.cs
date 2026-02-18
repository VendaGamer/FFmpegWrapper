namespace FFmpegWrapper.Codecs.Decoding;

using System.Runtime.InteropServices;

using Media;

/// <summary>
/// Decodes audio files
/// </summary>
public class AudioDecoder : MediaDecoder
{
    
    public AVSampleFormat SampleFormat {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Handle.Ref.sample_fmt;
    }

    public ref int SampleRate {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref Handle.Ref.sample_rate;
    }

    public int NumChannels {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Handle.Ref.ch_layout.nb_channels;
    }
    
    public ChannelLayout ChannelLayout {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(Handle.Ref.ch_layout);
    }

    public AudioFormat Format {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(SampleFormat, SampleRate, ChannelLayout);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public AudioDecoder(AVCodecID codecId)
        : this(MediaCodec.GetDecoder(codecId).Handle)
    {
        
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public AudioDecoder(NullableHandle<AVCodec> codec = default)
        : base(codec)
    {
        
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public AudioDecoder(Handle<AVCodecContext> ctx)
        : base(ctx)
    {
        
    }
}
