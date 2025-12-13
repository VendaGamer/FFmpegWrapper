namespace FFmpegWrapper.Codecs.Encoding;

using Hardware;

using Media;

public class VideoEncoder(FFHandle<AVCodecContext> ctx) : MediaEncoder(ctx)
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
    public int GopSize {
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
        get => Handle.Ref.max_b_frames;
        set {
            ThrowIfOpen();
            
            Handle.Ref.max_b_frames = value;
        }
    }

    public int MinQuantizer {
        get => Handle.Ref.qmin;
        set {
            ThrowIfOpen();
            
            Handle.Ref.qmin = value;
        }
    }
    public int MaxQuantizer {
        get => Handle.Ref.qmax;
        set {
            ThrowIfOpen();
            
            Handle.Ref.qmax = value;
        }
    }

    public VideoEncoder(AVCodecID codecId, in PictureFormat format, Rational frameRate, int bitrate = 0)
        : this(MediaCodec.GetEncoder(codecId), format, frameRate, bitrate) { }

    public VideoEncoder(MediaCodec codec, in PictureFormat format, Rational frameRate, int bitrate = 0)
        : this(AllocContext(codec))
    {
        FrameFormat = format;
        FrameRate = frameRate;
        TimeBase = frameRate.Reciprocal();
        BitRate = bitrate;
    }

    public VideoEncoder(CodecHardwareConfig config, in PictureFormat format, Rational frameRate, HardwareDevice device, HardwareFramePool? framePool = null)
        : this(config.Codec, in format, frameRate)
    {
        SetHardwareContext(config, device, framePool);
    }

    /// <summary> Returns the correct <see cref="MediaFrame.PresentationTimestamp"/> for the given frame number, in respect to <see cref="CodecBase.FrameRate"/> and <see cref="CodecBase.TimeBase"/>. </summary>
    public long GetFramePts(long frameNumber)
    {
        return av_rescale_q(frameNumber, av_inv_q(FrameRate), TimeBase);
    }
    
}
