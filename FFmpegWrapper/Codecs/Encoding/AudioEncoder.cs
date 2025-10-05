namespace FFmpegWrapper.Codecs.Encoding;

public class AudioEncoder : MediaEncoder
{
    public AVSampleFormat SampleFormat {
        get {
            unsafe
            {
                return Handle->sample_fmt;
            }
        }
        set {
            unsafe
            {
                SetOrThrowIfOpen(ref Handle->sample_fmt, value);
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
        set {
            unsafe
            {
                SetOrThrowIfOpen(ref Handle->sample_rate, value);
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
        set {
            unsafe
            {
                ThrowIfOpen();
                value.CopyTo(&Handle->ch_layout);
            }
        }
    }

    public AudioFormat Format {
        get {
            unsafe
            {
                ThrowIfOpen();
                ThrowIfDisposed();
                
                return new AudioFormat(_handle->sample_fmt, _handle->sample_rate,
                    ChannelLayout.FromHandle(&_handle->ch_layout));
            }
        }
        set {
            unsafe
            {
                ThrowIfOpen();
                ThrowIfDisposed();
                
                _handle->sample_rate = value.SampleRate;
                _handle->sample_fmt = value.SampleFormat;
                value.Layout.CopyTo(&_handle->ch_layout);
            }
        }
    }

    /// <summary> Number of samples per channel in an audio frame (set after the encoder is opened). </summary>
    /// <remarks>
    /// Each submitted frame except the last must contain exactly this amount of samples per channel.
    /// May be 0 when the codec has <see cref="MediaCodecCaps.VariableFrameSize"/> set, then the frame size is not restricted.
    /// </remarks>
    public int FrameSize {
        get {
            unsafe
            {
                return Handle->frame_size;
            }
        }
    }

    public AudioEncoder(AVCodecID codecId, in AudioFormat format, int bitrate = 0)
        : this(MediaCodec.GetEncoder(codecId), format, bitrate) { }

    public unsafe AudioEncoder(MediaCodec codec, in AudioFormat format, int bitrate = 0)
        : this(AllocContext(codec), takeOwnership: true)
    {
        Format = format;
        BitRate = bitrate;
        TimeBase = new Rational(1, format.SampleRate);
    }

    public unsafe AudioEncoder(AVCodecContext* ctx, bool takeOwnership)
        : base(ctx, MediaTypes.Audio, takeOwnership) { }
}