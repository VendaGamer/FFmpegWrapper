namespace FFmpegWrapper.Hardware;


public unsafe class HardwareFramePool : OwnedObject<AVBufferRef>
{
    public AVHWFramesContext* RawHandle {
        get {
            ThrowIfDisposed();
            return (AVHWFramesContext*)_handle->data;
        }
    }

    public int Width => RawHandle->width;
    public int Height => RawHandle->height;

    /// <inheritdoc cref="AVHWFramesContext.format" />
    public AVPixelFormat HwFormat => RawHandle->format;

    /// <inheritdoc cref="AVHWFramesContext.sw_format" />
    public AVPixelFormat SwFormat => RawHandle->sw_format;

    public HardwareFramePool(AVBufferRef* deviceCtx)
    {
        _handle = deviceCtx;
    }

    /// <summary> Allocate a new frame attached to the current hardware frame pool. </summary>
    public VideoFrame AllocFrame()
    {
        var frame = av_frame_alloc();
        int err = av_hwframe_get_buffer(_handle, frame, 0);
        if (err < 0) {
            av_frame_free(&frame);
            err.ThrowError(msg: "Failed to allocate hardware frame");
        }
        return new VideoFrame(frame);
    }

    protected override void Free()
    {
        if (_handle != null) {
            fixed (AVBufferRef** ppCtx = &_handle) {
                av_buffer_unref(ppCtx);
            }
        }
    }
}
