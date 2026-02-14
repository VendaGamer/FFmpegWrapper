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

    public override Handle<AVHWFramesContext> Handle {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Buffer.Data;
    }

    public MediaBuffer<AVHWFramesContext> Buffer {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            unsafe {
                return new MediaBuffer<AVHWFramesContext>(_handle);
            }
        }
    }

    internal readonly unsafe AVBufferRef* _handle;

    private bool _isInit;

    public HardwareFramePool(Handle<AVBufferRef> deviceCtx, HWPictureFormat format) : this(deviceCtx)
    {
        unsafe {
            av_hwframe_ctx_init(deviceCtx);
            _isInit = true;
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HardwareFramePool(Handle<AVBufferRef> deviceCtx)
    {
        unsafe {
            _handle = av_hwframe_ctx_alloc(deviceCtx);

            if (_handle is null)
                throw new Exception("Failed to allocate hardware frame pool");
        }
    }

    /// <summary> Allocate a new frame attached to the current hardware frame pool. </summary>
    public VideoFrame AllocFrame()
    {
        unsafe {
            var frame = av_frame_alloc();
            int err = av_hwframe_get_buffer(_handle, frame, 0);
            if (err < 0) {
                av_frame_free(&frame);
                err.ThrowError(msg: "Failed to allocate hardware frame");
            }
            return new VideoFrame(frame);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected override unsafe void Free()
    {
        if (_handle is not null) {
            fixed (AVBufferRef** ppCtx = &_handle) {
                av_buffer_unref(ppCtx);
            }
        }
    }
}
