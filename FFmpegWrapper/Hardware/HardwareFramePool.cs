namespace FFmpegWrapper.Hardware;


public sealed class HardwareFramePool : FFObjectBase<AVHWFramesContext>
{
    public int Width {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Handle.Ref.width;
    }

    public int Height {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Handle.Ref.height;
    }

    /// <inheritdoc cref="AVHWFramesContext.format" />
    public AVPixelFormat HWFormat {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Handle.Ref.format;
    }

    /// <inheritdoc cref="AVHWFramesContext.sw_format" />
    public AVPixelFormat SWFormat {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Handle.Ref.sw_format;
    }

    public ReadOnlySpan<AVPixelFormat> TransferToFormats {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => GetFormats(AVHWFrameTransferDirection.AV_HWFRAME_TRANSFER_DIRECTION_TO);
    }
    
    public ReadOnlySpan<AVPixelFormat> TransferFromFormats {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => GetFormats(AVHWFrameTransferDirection.AV_HWFRAME_TRANSFER_DIRECTION_FROM);
    }

    public override Handle<AVHWFramesContext> Handle {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Buffer.Data;
    }

    public readonly MediaBuffer<AVHWFramesContext> Buffer;
    private bool _isInit;

    public HardwareFramePool(MediaBuffer<AVHWDeviceContext> deviceCtx, HWPictureFormat format) : this(deviceCtx)
    {
        unsafe {
            av_hwframe_ctx_init(Buffer.Handle);
            _isInit = true;
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HardwareFramePool(MediaBuffer<AVHWDeviceContext> deviceCtx)
    {
        unsafe {
            var allocated = av_hwframe_ctx_alloc(deviceCtx.Handle);
            if (allocated is null)
                throw new Exception($"Could not allocate {nameof(AVHWFramesContext)}");
            
            Buffer = new MediaBuffer<AVHWFramesContext>(
                new Handle<AVBufferRef>(allocated, new SkipValidation())
            );
        }
    }

    /// <summary> Allocate a new frame attached to the current hardware frame pool. </summary>
    public VideoFrame AllocFrame()
    {
        unsafe {
            var frame = av_frame_alloc();
            if (frame is null)
                goto COULD_NOT_ALLOC;
            
            var res = av_hwframe_get_buffer(Buffer.Handle, frame, 0);
            if (res < 0)
                goto COULD_NOT_ALLOC;

            return new VideoFrame();
            
            COULD_NOT_ALLOC:
            av_frame_free(&frame);
            throw new Exception("Failed to allocate hardware frame");
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected override unsafe void Free()
    {
        fixed (AVBufferRef** ppCtx = &Buffer._handle) {
            av_buffer_unref(ppCtx);
        }
    }

    private ReadOnlySpan<AVPixelFormat> GetFormats(AVHWFrameTransferDirection direction)
    {
        unsafe {
            AVPixelFormat* formats = null;
            var res = av_hwframe_transfer_get_formats(Buffer._handle, direction, &formats, 0);

            if (res < 0)
                return ReadOnlySpan<AVPixelFormat>.Empty;

            return FFHelper.GetSpanFromSentinelTerminatedPtr(formats, AVPixelFormat.AV_PIX_FMT_NONE);
        }
    }
    
}
