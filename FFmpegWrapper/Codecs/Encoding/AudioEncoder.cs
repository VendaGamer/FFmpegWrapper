namespace FFmpegWrapper.Codecs.Encoding;

public class AudioEncoder : MediaEncoder
{
    public AVSampleFormat SampleFormat {
        get => Handle.Ref.sample_fmt;
        set {
            unsafe
            {
                ThrowIfOpen();
                Handle.Ref.sample_fmt = value;
            }
        }
    }

    public int SampleRate {
        get => Handle.Ref.sample_rate;
        set => Handle.Ref.sample_rate = value;
    }

    public ChannelLayout ChannelLayout {
        get => new(Handle.Ref.ch_layout);
        set {
            ThrowIfOpen();
            Handle.Ref.ch_layout = value.Native;
        }
    }

    public AudioFormat Format {
        get {
            unsafe
            {
                ThrowIfOpen();
                ThrowIfDisposed();
                
                return new AudioFormat(_handle->sample_fmt, _handle->sample_rate,
                    new ChannelLayout(_handle->ch_layout));
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
    public int FrameSize => Handle.Ref.frame_size;
    
    public AudioEncoder(FFHandle<AVCodecContext> ctx) : base(ctx) { }

    public AudioEncoder(AVCodecID codecId, in AudioFormat format, int bitrate = 0)
        : this(MediaCodec.GetEncoder(codecId).Handle, format, bitrate) { }

    public AudioEncoder(NullableFFHandle<AVCodec> codec, in AudioFormat format, int bitrate = 0)
        : base(codec)
    {
        Format = format;
        BitRate = bitrate;
        TimeBase = new Rational(1, format.SampleRate);
    }
    
}
