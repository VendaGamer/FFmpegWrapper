namespace FFmpegWrapper.Codecs.Encoding;

using Hardware;

using Media;

public unsafe class VideoEncoder : MediaEncoder
{
    public int Width {
        get => Handle.Ref.width;
        set {
            ThrowIfDisposed();
            Handle.Ref.width = value;
        }
    }
    public int Height {
        get => Handle.Ref.height;
        set {
            ThrowIfOpen();
            Handle.Ref.height = value;
        }
    }
    public AVPixelFormat PixelFormat {
        get => Handle.Ref.pix_fmt;
        set {
            ThrowIfOpen();
            Handle.Ref.pix_fmt = value;
        }
    }

    public PictureFormat FrameFormat {
        get => new(Width, Height, PixelFormat, _handle->sample_aspect_ratio);
        set {
            ThrowIfOpen();
            _handle->width = value.Width;
            _handle->height = value.Height;
            _handle->pix_fmt = value.PixelFormat;
        }
    }

    public PictureColorspace Colorspace {
        get {
            ref var handle = ref Handle.Ref;
            
            return new PictureColorspace(handle.colorspace, handle.color_primaries,
                handle.color_trc, handle.color_range);
            
        }
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
        get => Handle.Ref.gop_size;
        set {
            ThrowIfOpen();
            
            ref var handle = ref Handle.Ref;

            handle.gop_size = value;
        }
    }
    /// <inheritdoc cref="AVCodecContext.max_b_frames"/>
    public int MaxBFrames {
        get => _handle->max_b_frames;
        set => SetOrThrowIfOpen(ref _handle->max_b_frames, value);
    }

    public int MinQuantizer {
        get => _handle->qmin;
        set => SetOrThrowIfOpen(ref _handle->qmin, value);
    }
    public int MaxQuantizer {
        get => _handle->qmax;
        set => SetOrThrowIfOpen(ref _handle->qmax, value);
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

    public VideoEncoder(FFHandle<AVCodecContext> ctx) : base(ctx)
    {
        
    }

    /// <summary> Returns the correct <see cref="MediaFrame.PresentationTimestamp"/> for the given frame number, in respect to <see cref="CodecBase.FrameRate"/> and <see cref="CodecBase.TimeBase"/>. </summary>
    public long GetFramePts(long frameNumber)
    {
        return av_rescale_q(frameNumber, av_inv_q(FrameRate), TimeBase);
    }
    
}
