﻿namespace FFmpeg.Wrapper;

public unsafe class VideoDecoder : MediaDecoder
{
    public int Width => _handle->width;
    public int Height => _handle->height;
    public AVPixelFormat PixelFormat => _handle->pix_fmt;

    public PictureFormat FrameFormat => new(Width, Height, PixelFormat, _handle->sample_aspect_ratio);
    public PictureColorspace Colorspace => new(_handle->colorspace, _handle->color_primaries, _handle->color_trc, _handle->color_range);

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
        SetHardwareContext(config, device, null);
        //TODO: support custom decoder negotiation and hw_frames_ctx

        _handle->get_format = _chooseHwPixelFmt = (ctx, pAvailFmts) => {
            for (var pFmt = pAvailFmts; *pFmt != PixelFormats.None; pFmt++) {
                if (*pFmt == config.PixelFormat) {
                    return *pFmt;
                }
            }
            return ctx->sw_pix_fmt;
        };
    }

    /// <summary> Returns a new list containing all hardware configurations that may work with the current codec. </summary>
    public List<CodecHardwareConfig> GetHardwareConfigs()
    {
        ThrowIfDisposed();

        var configs = new List<CodecHardwareConfig>();

        int i = 0;
        AVCodecHWConfig* config;

        while ((config = ffmpeg.avcodec_get_hw_config(_handle->codec, i++)) != null) {
            if ((config->methods & (int)CodecHardwareMethods.DeviceContext) != 0) {
                configs.Add(new CodecHardwareConfig(_handle->codec, config));
            }
        }
        return configs;
    }
}