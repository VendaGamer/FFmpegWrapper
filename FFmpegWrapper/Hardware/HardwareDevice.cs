namespace FFmpeg.Wrapper;

public unsafe class HardwareDevice : FFObject<AVBufferRef>
{
    public AVHWDeviceContext* RawHandle {
        get {
            ThrowIfDisposed();
            return (AVHWDeviceContext*)_handle->data;
        }
    }

    public AVHWDeviceType Type => RawHandle->type;

    public HardwareDevice(AVBufferRef* deviceCtx)
    {
        _handle = deviceCtx;
    }

    /// <summary> Open a device of the specified type and create a context for it. </summary>
    /// <returns> The created device context or null on failure. </returns>
    public static HardwareDevice? Create(AVHWDeviceType type)
    {
        AVBufferRef* ctx;
        if (ffmpeg.av_hwdevice_ctx_create(&ctx, type, null, null, 0) < 0) {
            return null;
        }
        return new HardwareDevice(ctx);
    }

    /// <inheritdoc cref="ffmpeg.av_hwdevice_get_hwframe_constraints(AVBufferRef*, void*)"/>
    public HardwareFrameConstraints? GetMaxFrameConstraints()
    {
        var desc = ffmpeg.av_hwdevice_get_hwframe_constraints(_handle, null);

        if (desc == null) {
            return null;
        }
        var managedDesc = new HardwareFrameConstraints(desc);
        ffmpeg.av_hwframe_constraints_free(&desc);
        return managedDesc;
    }

    /// <param name="swFormat"> The pixel format identifying the actual data layout of the hardware frames. </param>
    /// <param name="initialSize"> Initial size of the frame pool. If a device type does not support dynamically resizing the pool, then this is also the maximum pool size. </param>
    public HardwareFramePool? CreateFramePool(PictureFormat swFormat, int initialSize)
    {
        ThrowIfDisposed();

        var poolRef = ffmpeg.av_hwframe_ctx_alloc(_handle);
        if (poolRef == null) {
            throw new OutOfMemoryException("Failed to allocate hardware frame pool");
        }
        var pool = (AVHWFramesContext*)poolRef->data;
        pool->format = GetDefaultSurfaceFormat();
        pool->sw_format = swFormat.PixelFormat;
        pool->width = swFormat.Width;
        pool->height = swFormat.Height;
        pool->initial_pool_size = initialSize;

        if (ffmpeg.av_hwframe_ctx_init(poolRef) < 0) {
            ffmpeg.av_buffer_unref(&poolRef);
            return null;
        }
        return new HardwareFramePool(poolRef);
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

    protected override void Free()
    {
        if (_handle != null) {
            fixed (AVBufferRef** ppCtx = &_handle) {
                ffmpeg.av_buffer_unref(ppCtx);
            }
        }
    }
}