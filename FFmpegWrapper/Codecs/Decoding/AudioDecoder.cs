namespace FFmpegWrapper.Codecs.Decoding;

using System.Runtime.InteropServices;

/// <summary>
/// Decodes audio files
/// </summary>
public class AudioDecoder : MediaDecoder
{
    
    
    public AVSampleFormat SampleFormat {
        get {
            unsafe
            {
                return Handle.Ref.sample_fmt;
            }
        }
    }

    public int SampleRate {
        get => Handle.Ref.sample_rate;
        set => Handle.Ref.sample_rate = value;
    }

    public int NumChannels => Handle.Ref.ch_layout.nb_channels;

    public ChannelLayout ChannelLayout => new(Handle.Ref.ch_layout);

    public AudioFormat Format => new(SampleFormat, SampleRate, ChannelLayout);

    public AudioDecoder(AVCodecID codecId)
        : this(MediaCodec.GetDecoder(codecId).Handle)
    {
        
    }

    public AudioDecoder(NullableFFHandle<AVCodec> codec = default)
        : base(codec)
    {
        
    }
    
    public AudioDecoder(FFHandle<AVCodecContext> ctx)
        : base(ctx)
    {
        
    }
}
