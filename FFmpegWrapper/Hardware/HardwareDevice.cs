namespace FFmpegWrapper.Hardware;

using Abstractions;

using Codecs;

using Extensions;

/// <summary>
/// Wrapper of Hardware Device
/// </summary>
public sealed class HardwareDevice : FFObject<AVBufferRef>
{
    
    #region Static Members

    private static ImmutableArray<AVHWDeviceType> avaliableDeviceTypes;
    /// <summary>
    /// Gets AVHWDeviceTypes that are supported by used ffmpeg libraries
    /// </summary>
    public static ImmutableArray<AVHWDeviceType> AvaliableDeviceTypes {
        get {
            if (avaliableDeviceTypes.IsDefault) {
                avaliableDeviceTypes = GetAvailableHwDeviceTypes();
            }
            return avaliableDeviceTypes;
        }
    }
    
    /// <summary>
    /// Hardware device types ordered by priority (first = highest priority).
    /// CUDA is preferred, followed by other high-performance options.
    /// </summary>
    public static readonly AVHWDeviceType[] HardwareDevicePriority =
    [
        (AVHWDeviceType)13,                             // NVIDIA NVDEC - hardware decoder
        AVHWDeviceType.AV_HWDEVICE_TYPE_CUDA,           // NVIDIA CUDA - best performance
        AVHWDeviceType.AV_HWDEVICE_TYPE_D3D11VA,        // Direct3D 11 Video Acceleration
        AVHWDeviceType.AV_HWDEVICE_TYPE_DXVA2,          // DirectX Video Acceleration 2.0
        AVHWDeviceType.AV_HWDEVICE_TYPE_VAAPI,          // Video Acceleration API (Intel/AMD on Linux)
        AVHWDeviceType.AV_HWDEVICE_TYPE_VIDEOTOOLBOX,   // Apple VideoToolbox
        AVHWDeviceType.AV_HWDEVICE_TYPE_QSV,            // Intel Quick Sync Video
        AVHWDeviceType.AV_HWDEVICE_TYPE_OPENCL,         // OpenCL acceleration
        AVHWDeviceType.AV_HWDEVICE_TYPE_VULKAN          // Vulkan compute
    ];
    
    private static ImmutableArray<AVHWDeviceType> GetAvailableHwDeviceTypes()
    {
        var builder = ImmutableArray.CreateBuilder<AVHWDeviceType>();
        AVHWDeviceType type = AVHWDeviceType.AV_HWDEVICE_TYPE_NONE;
        
        while ((type = av_hwdevice_iterate_types(type)) != AVHWDeviceType.AV_HWDEVICE_TYPE_NONE)
        {
            builder.Add(type);
        }
        
        return builder.ToImmutable();
    }
    
    /// <summary>
    /// Gets the priority index for a hardware device type.
    /// Lower values indicate higher priority.
    /// </summary>
    private static int GetDeviceTypePriority(AVHWDeviceType deviceType)
    {
        for (int i = 0; i < HardwareDevicePriority.Length; i++)
        {
            if (HardwareDevicePriority[i] == deviceType)
                return i;
        }
            
        // Unknown device types get lowest priority
        return int.MaxValue;
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
        var availableConfigs = CodecHardwareConfig.GetHardwareConfigs(codecId);
        
        // Iterate through our priority list, from highest to lowest priority.
        foreach (var preferredDeviceType in HardwareDevicePriority)
        {
            // Find the first available config that matches the current priority level.
            var config = availableConfigs.FirstOrDefault(c => c.DeviceType == preferredDeviceType);

            if (config.Handle.IsNull) {
                continue;
            }
            // Attempt to create the hardware device. A 'using' block ensures it's disposed if not returned.
            if (!TryCreate(config.DeviceType, out var hardwareDevice))
            {
                continue;
            }
            
            // Check if the device can handle the target format.
            if (hardwareDevice.FrameConstraints == null ||
                hardwareDevice.FrameConstraints.IsValidFormat(targetFormat))
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

    #endregion

    
    public unsafe AVHWDeviceContext* CtxHandle {
        get {
            return (AVHWDeviceContext*)Handle.Ref.data;
        }
    }

    public AVHWDeviceType Type {
        get {
            unsafe
            {
                return CtxHandle->type;
            }
        }
    }

    internal unsafe HardwareDevice(AVBufferRef* deviceCtx)
    {
        _handle = deviceCtx;
        
        var desc = av_hwdevice_get_hwframe_constraints(_handle, null);
        if (desc is not null) {
            FrameConstraints = new HardwareFrameConstraints(desc);
        }

    }

    /// <summary> Open a device of the specified type and create a context for it. </summary>
    /// <returns> The created device context or null on failure. </returns>
    public static bool TryCreate(AVHWDeviceType type, out HardwareDevice device)
    {
        unsafe
        {
            AVBufferRef* ctx;
            if (av_hwdevice_ctx_create(&ctx, type, null, null, 0) < 0) {
                device = null!;
                return false;
            }
            device = new HardwareDevice(ctx);
            return true;
        }
    }

    public readonly HardwareFrameConstraints? FrameConstraints;

    /// <param name="swFormat"> The pixel format identifying the actual data layout of the hardware frames. </param>
    /// <param name="initialSize"> Initial size of the frame pool. If a device type does not support dynamically resizing the pool, then this is also the maximum pool size. </param>
    public HardwareFramePool? CreateFramePool(PictureFormat swFormat, int initialSize)
    {
        unsafe
        {
            var poolRef = av_hwframe_ctx_alloc(Handle);
            if (poolRef == null) {
                throw new OutOfMemoryException("Failed to allocate hardware frame pool");
            }
            var pool = (AVHWFramesContext*)poolRef->data;
            pool->format = GetDefaultSurfaceFormat();
            pool->sw_format = swFormat.PixelFormat;
            pool->width = swFormat.Width;
            pool->height = swFormat.Height;
            pool->initial_pool_size = initialSize;

            if (av_hwframe_ctx_init(poolRef) < 0) {
                av_buffer_unref(&poolRef);
                return null;
            }
            return new HardwareFramePool(poolRef);
        }
    }

    private AVPixelFormat GetDefaultSurfaceFormat()
    {
        return Type switch {
            HWDeviceTypes.VDPAU => AVPixelFormat.AV_PIX_FMT_VDPAU,
            HWDeviceTypes.Cuda  => AVPixelFormat.AV_PIX_FMT_CUDA,
            HWDeviceTypes.VAAPI => AVPixelFormat.AV_PIX_FMT_VAAPI,
            HWDeviceTypes.DXVA2 => AVPixelFormat.AV_PIX_FMT_DXVA2_VLD,
            HWDeviceTypes.QSV   => AVPixelFormat.AV_PIX_FMT_QSV,
            HWDeviceTypes.D3D11VA => AVPixelFormat.AV_PIX_FMT_D3D11,
            HWDeviceTypes.D3D12VA => AVPixelFormat.AV_PIX_FMT_D3D12,
            HWDeviceTypes.DRM   => AVPixelFormat.AV_PIX_FMT_DRM_PRIME,
            HWDeviceTypes.OpenCL => AVPixelFormat.AV_PIX_FMT_OPENCL,
            HWDeviceTypes.Vulkan => AVPixelFormat.AV_PIX_FMT_VULKAN,
            HWDeviceTypes.VideoToolbox => AVPixelFormat.AV_PIX_FMT_VIDEOTOOLBOX,
            HWDeviceTypes.MediaCodec => AVPixelFormat.AV_PIX_FMT_MEDIACODEC,
            _ => AVPixelFormat.AV_PIX_FMT_YUV420P
        };
    }

    /// <inheritdoc />
    protected override void Free()
    {
        unsafe
        {
            if (_handle != null) {
                fixed (AVBufferRef** ppCtx = &_handle) {
                    av_buffer_unref(ppCtx);
                }
            }
        }
    }
}
