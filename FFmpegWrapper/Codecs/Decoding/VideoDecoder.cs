namespace FFmpegWrapper.Codecs.Decoding;

using Hardware;
using Media;

public unsafe class VideoDecoder : MediaDecoder
{
    public int Width => Handle->width;
    public int Height => Handle->height;
    public AVPixelFormat PixelFormat => Handle->pix_fmt;
    public PictureFormat FrameFormat => new(Width, Height, PixelFormat, Handle->sample_aspect_ratio);
    public PictureColorspace Colorspace {
        get {
            ThrowIfDisposed();
            
            return new PictureColorspace(handle->colorspace, handle->color_primaries,
                handle->color_trc, handle->color_range);
        }
    }

    public VideoDecoder(AVCodecID codecId)
        : this(MediaCodec.GetDecoder(codecId)) { }

    public VideoDecoder(MediaCodec codec)
        : this(AllocContext(codec), takeOwnership: true) { }

    public VideoDecoder(AVCodecContext* ctx, bool takeOwnership)
        : base(ctx, MediaTypes.Video, takeOwnership) { }

    //Used to prevent callback pointer from being GC collected
    AVCodecContext_get_format? _chooseHwPixelFmt;

    /// <summary>
    /// Before the decoder is open, setups hardware acceleration via the specified device. 
    /// If the device does not support the input format, a software decoder will be used instead.
    /// </summary>
    public void SetupHardwareAccelerator(CodecHardwareConfig config, HardwareDevice device)
    {
        ThrowIfOpen();
        ThrowIfDisposed();
        
        SetHardwareContext(config, device, null);
        //TODO: support custom decoder negotiation and hw_frames_ctx

        handle->get_format = _chooseHwPixelFmt = (ctx, pAvailFmts) => {
            for (var pFmt = pAvailFmts; *pFmt != PixelFormats.None; pFmt++) {
                if (*pFmt == config.PixelFormat) {
                    return *pFmt;
                }
            }
            return ctx->sw_pix_fmt;
        };
    }
    
}