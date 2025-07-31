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
                return Handle->sample_fmt;
            }
        }
    }

    public int SampleRate {
        get {
            unsafe
            {
                return Handle->sample_rate;
            }
        }
    }

    public int NumChannels {
        get {
            unsafe
            {
                return Handle->ch_layout.nb_channels;
            }
        }
    }

    public ChannelLayout ChannelLayout {
        get {
            unsafe
            {
                return ChannelLayout.FromHandle(&Handle->ch_layout);
            }
        }
    }

    public AudioFormat Format => new(SampleFormat, SampleRate, ChannelLayout);

    public AudioDecoder(AVCodecID codecId)
        : this(MediaCodec.GetDecoder(codecId)) { }

    public unsafe AudioDecoder(MediaCodec codec)
        : this(AllocContext(codec), takeOwnership: true) { }

    public unsafe AudioDecoder(AVCodecContext* ctx, bool takeOwnership)
        : base(ctx, MediaTypes.Audio, takeOwnership) { }
}