namespace FFmpegWrapper.Codecs.Decoding;

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
        : this(MediaCodec.GetDecoder(codecId)) { }

    public unsafe AudioDecoder(MediaCodec codec)
        : this(AllocContext(codec), takeOwnership: true) { }

    public unsafe AudioDecoder(AVCodecContext* ctx, bool takeOwnership)
        : base(ctx, MediaTypes.Audio, takeOwnership) { }
}