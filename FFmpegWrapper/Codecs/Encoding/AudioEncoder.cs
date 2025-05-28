namespace FFmpeg.Wrapper;

public unsafe class AudioEncoder : MediaEncoder
{
    public AVSampleFormat SampleFormat {
        get => _handle->sample_fmt;
        set => SetOrThrowIfOpen(ref _handle->sample_fmt, value);
    }
    public int SampleRate {
        get => _handle->sample_rate;
        set => SetOrThrowIfOpen(ref _handle->sample_rate, value);
    }
    public int NumChannels => _handle->ch_layout.nb_channels;
    public ChannelLayout ChannelLayout {
        get => ChannelLayout.FromExisting(&_handle->ch_layout);
        set {
            ThrowIfOpen();
            value.CopyTo(&_handle->ch_layout);
        }
    }

    public AudioFormat Format {
        get => new(SampleFormat, SampleRate, ChannelLayout);
        set {
            ThrowIfOpen();
            _handle->sample_rate = value.SampleRate;
            _handle->sample_fmt = value.SampleFormat;
            value.Layout.CopyTo(&_handle->ch_layout);
        }
    }

    /// <summary> Number of samples per channel in an audio frame (set after the encoder is opened). </summary>
    /// <remarks>
    /// Each submitted frame except the last must contain exactly this amount of samples per channel.
    /// May be null when the codec has <see cref="MediaCodecCaps.VariableFrameSize"/> set, then the frame size is not restricted.
    /// </remarks>
    public int? FrameSize => _handle->frame_size == 0 ? null : _handle->frame_size;

    public AudioEncoder(AVCodecID codecId, in AudioFormat format, int bitrate = 0)
        : this(MediaCodec.GetEncoder(codecId), format, bitrate) { }

    public AudioEncoder(MediaCodec codec, in AudioFormat format, int bitrate = 0)
        : this(AllocContext(codec), takeOwnership: true)
    {
        Format = format;
        BitRate = bitrate;
        TimeBase = new Rational(1, format.SampleRate);
    }

    public AudioEncoder(AVCodecContext* ctx, bool takeOwnership)
        : base(ctx, MediaTypes.Audio, takeOwnership) { }
}