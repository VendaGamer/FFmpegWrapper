namespace FFmpegWrapper.Codecs.Encoding;

public class AudioEncoder : MediaEncoder
{
    public AVSampleFormat SampleFormat {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Handle.Ref.sample_fmt;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
            ThrowIfOpen();
            Handle.Ref.sample_fmt = value;
        }
    }

    public int SampleRate {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Handle.Ref.sample_rate;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => Handle.Ref.sample_rate = value;
    }

    public ChannelLayout ChannelLayout {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(Handle.Ref.ch_layout);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set {
            ThrowIfOpen();
            Handle.Ref.ch_layout = value.Native;
        }
    }

    public AudioFormat Format {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ref var handle = ref Handle.Ref;
                
            return new AudioFormat(
                handle.sample_fmt,
                handle.sample_rate,
                handle.ch_layout);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
            ThrowIfOpen();
            ref var handle = ref Handle.Ref;
                
            handle.sample_rate = value.SampleRate;
            handle.sample_fmt = value.SampleFormat;
            handle.ch_layout = value.Layout.Native;
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
