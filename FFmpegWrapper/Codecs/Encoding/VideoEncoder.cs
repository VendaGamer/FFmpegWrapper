namespace FFmpegWrapper.Codecs.Encoding;

using Hardware;

using Media;

public unsafe class VideoEncoder : MediaEncoder
{
    public int Width {
        get => _handle->width;
        set => SetOrThrowIfOpen(ref _handle->width, value);
    }
    public int Height {
        get => _handle->height;
        set => SetOrThrowIfOpen(ref _handle->height, value);
    }
    public AVPixelFormat PixelFormat {
        get => _handle->pix_fmt;
        set => SetOrThrowIfOpen(ref _handle->pix_fmt, value);
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
        get => new(_handle->colorspace, _handle->color_primaries, _handle->color_trc, _handle->color_range);
        set {
            ThrowIfOpen();
            _handle->colorspace = value.Matrix;
            _handle->color_primaries = value.Primaries;
            _handle->color_trc = value.Transfer;
            _handle->color_range = value.Range;
        }
    }

    /// <inheritdoc cref="AVCodecContext.gop_size"/>
    public int GopSize {
        get => _handle->gop_size;
        set => SetOrThrowIfOpen(ref _handle->gop_size, value);
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