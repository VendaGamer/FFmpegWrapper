namespace FFmpegWrapper.Hardware;

using System.Diagnostics.CodeAnalysis;
using Codecs;

/// <summary>
/// Wrapper of Hardware Device
/// </summary>
public sealed class HardwareDevice : FFBufferObject<AVHWDeviceContext>
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
    public static readonly IReadOnlyList<AVHWDeviceType> HardwareDevicePriority =
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
        for (int i = 0; i < HardwareDevicePriority.Count; i++)
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
        var availableConfigs = CodecHardwareConfig.GetAvailableConfigsFor(MediaCodec.GetDecoder(codecId).Handle);
        
        foreach (var preferredDeviceType in HardwareDevicePriority)
        {
            foreach (var config in availableConfigs) {
                if (config.DeviceType != preferredDeviceType) {
                    continue;
                }

                if (!TryCreate(config.DeviceType, out var hardwareDevice))
                {
                    continue;
                }
                
                if (hardwareDevice.FrameConstraints is null || hardwareDevice.FrameConstraints.IsValidSoftwareFormat(targetFormat))
                {
                    codecConfig = config;
                    device = hardwareDevice;
                    return true;
                }

                break;
            }
        }
        
        device = null!;
        codecConfig = default;
        return false;
    }

    #endregion

    public AVHWDeviceType Type {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Handle.Ref.type;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe HardwareDevice(MediaBuffer<AVHWDeviceContext> deviceCtx)
    {
        _handle = deviceCtx.Handle;

        HardwareFrameConstraints.TryCreate(new MediaBuffer<AVHWDeviceContext>(deviceCtx.Handle),out FrameConstraints);
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
            
            device = new HardwareDevice(*(MediaBuffer<AVHWDeviceContext>*)&ctx);
            return true;
        }
    }

    public readonly HardwareFrameConstraints? FrameConstraints;

    /// <param name="swFormat"> The pixel format identifying the actual data layout of the hardware frames. </param>
    /// <param name="initialSize"> Initial size of the frame pool. If a device type does not support dynamically resizing the pool, then this is also the maximum pool size. </param>
    public bool TryCreateFramePool(
        PictureFormat swFormat,
        int initialSize,
        [NotNullWhen(true)]
        out HardwareFramePool? pool)
    {
        unsafe
        {
            var poolRef = av_hwframe_ctx_alloc(Buffer.Handle);
            if (poolRef is null)
                goto Fail;

            var handle = (AVHWFramesContext*)poolRef->data;
            
            handle->format = GetDefaultSurfaceFormat();
            handle->sw_format = swFormat.PixelFormat;
            handle->width = swFormat.Width;
            handle->height = swFormat.Height;
            handle->initial_pool_size = initialSize;

            if (av_hwframe_ctx_init(poolRef) < 0)
                goto Fail;
            
            pool = new HardwareFramePool(*(MediaBuffer<AVHWDeviceContext>*)&poolRef);
            return true;
            
            Fail:
            pool = null;
            return false;
        }
    }

    private AVPixelFormat GetDefaultSurfaceFormat()
    {
        return Type switch {
            AVHWDeviceType.AV_HWDEVICE_TYPE_VDPAU => AVPixelFormat.AV_PIX_FMT_VDPAU,
            AVHWDeviceType.AV_HWDEVICE_TYPE_CUDA  => AVPixelFormat.AV_PIX_FMT_CUDA,
            AVHWDeviceType.AV_HWDEVICE_TYPE_VAAPI => AVPixelFormat.AV_PIX_FMT_VAAPI,
            AVHWDeviceType.AV_HWDEVICE_TYPE_DXVA2 => AVPixelFormat.AV_PIX_FMT_DXVA2_VLD,
            AVHWDeviceType.AV_HWDEVICE_TYPE_QSV   => AVPixelFormat.AV_PIX_FMT_QSV,
            AVHWDeviceType.AV_HWDEVICE_TYPE_D3D11VA => AVPixelFormat.AV_PIX_FMT_D3D11,
            AVHWDeviceType.AV_HWDEVICE_TYPE_D3D12VA => AVPixelFormat.AV_PIX_FMT_D3D12,
            AVHWDeviceType.AV_HWDEVICE_TYPE_DRM   => AVPixelFormat.AV_PIX_FMT_DRM_PRIME,
            AVHWDeviceType.AV_HWDEVICE_TYPE_OPENCL => AVPixelFormat.AV_PIX_FMT_OPENCL,
            AVHWDeviceType.AV_HWDEVICE_TYPE_VULKAN => AVPixelFormat.AV_PIX_FMT_VULKAN,
            AVHWDeviceType.AV_HWDEVICE_TYPE_VIDEOTOOLBOX => AVPixelFormat.AV_PIX_FMT_VIDEOTOOLBOX,
            AVHWDeviceType.AV_HWDEVICE_TYPE_MEDIACODEC => AVPixelFormat.AV_PIX_FMT_MEDIACODEC,
            AVHWDeviceType.AV_HWDEVICE_TYPE_AMF => AVPixelFormat.AV_PIX_FMT_AMF_SURFACE,
            AVHWDeviceType.AV_HWDEVICE_TYPE_OHCODEC => AVPixelFormat.AV_PIX_FMT_OHCODEC,
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
