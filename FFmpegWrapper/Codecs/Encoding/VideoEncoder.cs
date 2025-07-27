namespace FFmpegWrapper.Codecs.Encoding;

using Hardware;

public unsafe class VideoEncoder : MediaEncoder
{
    public int Width {
        get => handle->width;
        set => SetOrThrowIfOpen(ref handle->width, value);
    }
    public int Height {
        get => handle->height;
        set => SetOrThrowIfOpen(ref handle->height, value);
    }
    public AVPixelFormat PixelFormat {
        get => handle->pix_fmt;
        set => SetOrThrowIfOpen(ref handle->pix_fmt, value);
    }

    public PictureFormat FrameFormat {
        get => new(Width, Height, PixelFormat, handle->sample_aspect_ratio);
        set {
            ThrowIfOpen();
            handle->width = value.Width;
            handle->height = value.Height;
            handle->pix_fmt = value.PixelFormat;
        }
    }

    public PictureColorspace Colorspace {
        get => new(handle->colorspace, handle->color_primaries, handle->color_trc, handle->color_range);
        set {
            ThrowIfOpen();
            handle->colorspace = value.Matrix;
            handle->color_primaries = value.Primaries;
            handle->color_trc = value.Transfer;
            handle->color_range = value.Range;
        }
    }

    /// <inheritdoc cref="AVCodecContext.gop_size"/>
    public int GopSize {
        get => handle->gop_size;
        set => SetOrThrowIfOpen(ref handle->gop_size, value);
    }
    /// <inheritdoc cref="AVCodecContext.max_b_frames"/>
    public int MaxBFrames {
        get => handle->max_b_frames;
        set => SetOrThrowIfOpen(ref handle->max_b_frames, value);
    }

    public int MinQuantizer {
        get => handle->qmin;
        set => SetOrThrowIfOpen(ref handle->qmin, value);
    }
    public int MaxQuantizer {
        get => handle->qmax;
        set => SetOrThrowIfOpen(ref handle->qmax, value);
    }

    public VideoEncoder(AVCodecID codecId, in PictureFormat format, Rational frameRate, int bitrate = 0)
        : this(MediaCodec.GetEncoder(codecId), format, frameRate, bitrate) { }

    public VideoEncoder(MediaCodec codec, in PictureFormat format, Rational frameRate, int bitrate = 0)
        : this(AllocContext(codec), takeOwnership: true)
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

    public VideoEncoder(AVCodecContext* ctx, bool takeOwnership)
        : base(ctx, MediaTypes.Video, takeOwnership) { }

    /// <summary> Returns the correct <see cref="MediaFrame.PresentationTimestamp"/> for the given frame number, in respect to <see cref="CodecBase.FrameRate"/> and <see cref="CodecBase.TimeBase"/>. </summary>
    public long GetFramePts(long frameNumber)
    {
        return ffmpeg.av_rescale_q(frameNumber, ffmpeg.av_inv_q(FrameRate), TimeBase);
    }
    
}