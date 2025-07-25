namespace FFmpegWrapper.Codecs.Decoding;

using Core;

using Hardware;

using Media.Formats;

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
    
    
    /// <summary>
    /// Tries to find and create a hardware device suitable for decoding a specific codec and frame format.
    /// The search follows a predefined priority list of hardware acceleration types.
    /// </summary>
    /// <param name="codecId">The ID of the codec to be decoded.</param>
    /// <param name="targetFormat">The target picture format (resolution and pixel format) for the output frames.</param>
    /// <param name="device">When this method returns true, contains the created and ready-to-use HardwareDevice.</param>
    /// <param name="codecConfig">When this method returns true, contains the hardware configuration used to create the device.</param>
    /// <returns>true if a compatible hardware device was found and created; otherwise, false.</returns>
    public static bool TryCreateCompatibleHardwareDevice(
        AVCodecID codecId,
        in PictureFormat targetFormat,
        out HardwareDevice device,
        out CodecHardwareConfig codecConfig)
    {
        // Get all available hardware configurations for the specified codec just once.
        var availableConfigs = GetHardwareConfigs(codecId);

        // Iterate through our priority list, from highest to lowest priority.
        foreach (var preferredDeviceType in HardwareDevice.HardwareDevicePriority)
        {
            // Find the first available config that matches the current priority level.
            var config = availableConfigs.FirstOrDefault(c => c.DeviceType == preferredDeviceType);

            if (config.Handle == null) {
                continue;
            }
            // Attempt to create the hardware device. A 'using' block ensures it's disposed if not returned.
            if (!HardwareDevice.TryCreate(config.DeviceType, out var hardwareDevice))
            {
                continue;
            }

            var constraints = hardwareDevice.GetMaxFrameConstraints();

            // Check if the device can handle the target format.
            if (constraints == null || constraints.IsValidFormat(targetFormat))
            {
                codecConfig = config;
                device = hardwareDevice;
                return true;
            }
        }

        // No suitable device was found after checking all priorities.
        device = null!;
        codecConfig = default;
        return false;
    }
    
    /// <summary>
    /// Returns a list of all hardware decoder configurations that may or may not be supported on this machine.
    /// </summary>
    /// <param name="codecId">
    /// If specified, only configurations for that codec will be returned.
    /// </param>
    /// <param name="deviceType">
    /// If specified, only configurations for that device type will be returned.
    /// </param>
    public static unsafe List<CodecHardwareConfig> GetHardwareConfigs(
        AVCodecID? codecId = null,
        AVHWDeviceType? deviceType = null)
    {
        var configs = new List<CodecHardwareConfig>();
        void* iterState = null;
        AVCodec* codec;

        while ((codec = ffmpeg.av_codec_iterate(&iterState)) != null)
        {
            if ((codecId != null && codec->id != codecId) ||
                ffmpeg.av_codec_is_decoder(codec) == 0)
                continue;

            int i = 0;
            AVCodecHWConfig* configPtr;

            while ((configPtr = ffmpeg.avcodec_get_hw_config(codec, i++)) != null)
            {
                const int reqMethods = (int)(CodecHardwareMethods.DeviceContext | CodecHardwareMethods.FramesContext);

                if ((configPtr->methods & reqMethods) != 0 && 
                    (deviceType == null || configPtr->device_type == deviceType))
                {
                    configs.Add(new CodecHardwareConfig(codec, configPtr));
                }
            }
        }

        return configs;
    }
    
}