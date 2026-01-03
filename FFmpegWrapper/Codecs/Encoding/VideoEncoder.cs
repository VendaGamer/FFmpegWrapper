namespace FFmpegWrapper.Codecs.Encoding;

using Hardware;

using Media;

public class VideoEncoder : MediaEncoder
{
    public int Width {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Handle.Ref.width;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set {
            ThrowIfDisposed();
            Handle.Ref.width = value;
        }
    }
    public int Height {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Handle.Ref.height;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set {
            ThrowIfOpen();
            Handle.Ref.height = value;
        }
    }
    public AVPixelFormat PixelFormat {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Handle.Ref.pix_fmt;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set {
            ThrowIfOpen();
            Handle.Ref.pix_fmt = value;
        }
    }

    public PictureFormat FrameFormat {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(Width, Height, PixelFormat, Handle.Ref.sample_aspect_ratio);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set {

            ThrowIfOpen();
            ref var handle = ref Handle.Ref;
            
            handle.width = value.Width;
            handle.height = value.Height;
            handle.pix_fmt = value.PixelFormat;
        }
    }

    public PictureColorspace Colorspace {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            ref var handle = ref Handle.Ref;
            
            return new PictureColorspace(handle.colorspace, handle.color_primaries,
                handle.color_trc, handle.color_range);
            
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set {
            ThrowIfOpen();
            ref var handle = ref Handle.Ref;
            handle.colorspace = value.Matrix;
            handle.color_primaries = value.Primaries;
            handle.color_trc = value.Transfer;
            handle.color_range = value.Range;
        }
    }

    /// <inheritdoc cref="AVCodecContext.gop_size"/>
    public int GroupOfPicturesSize {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Handle.Ref.gop_size;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set {
            ThrowIfOpen();
            
            ref var handle = ref Handle.Ref;

            handle.gop_size = value;
        }
    }
    /// <inheritdoc cref="AVCodecContext.max_b_frames"/>
    public int MaxBFrames {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Handle.Ref.max_b_frames;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set {
            ThrowIfOpen();
            
            Handle.Ref.max_b_frames = value;
        }
    }

    public int MinQuantizer {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Handle.Ref.qmin;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set {
            ThrowIfOpen();
            
            Handle.Ref.qmin = value;
        }
    }
    public int MaxQuantizer {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Handle.Ref.qmax;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set {
            ThrowIfOpen();
            
            Handle.Ref.qmax = value;
        }
    }

    public VideoEncoder(FFHandle<AVCodecContext> ctx) : base(ctx)
    {
        
    }

    public VideoEncoder(AVCodecID codecId, in PictureFormat format, Rational frameRate, int bitrate = 0)
        : this(MediaCodec.GetEncoder(codecId).Handle, format, frameRate, bitrate) { }

    public unsafe VideoEncoder(NullableFFHandle<AVCodec> codec, in PictureFormat format, Rational frameRate, int bitrate = 0)
        : base(codec)
    {
        FrameFormat = format;
        FrameRate = frameRate;
        
        TimeBase = frameRate.Reciprocal();
        BitRate = bitrate;
    }

    public VideoEncoder(
        CodecHardwareConfig config,
        in PictureFormat format,
        Rational frameRate,
        HardwareDevice device,
        NullableFFHandle<AVBufferRef> framePool = default)
        : this(config.Codec.Handle, in format, frameRate)
    {
        SetHardwareContext(config, device, framePool);
    }

    /// <summary> Returns the correct <see cref="MediaFrame.PresentationTimestamp"/> for the given frame number, in respect to <see cref="CodecBase.FrameRate"/> and <see cref="CodecBase.TimeBase"/>. </summary>
    public long GetFramePts(long frameNumber)
    {
        return av_rescale_q(frameNumber, FrameRate.Reciprocal(), TimeBase);
    }
    
}
