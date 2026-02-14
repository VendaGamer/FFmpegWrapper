namespace FFmpegWrapper.Hardware;

using System.Diagnostics.CodeAnalysis;

public class HardwareFrameConstraints : FFObject<AVHWFramesConstraints>
{
    public ReadOnlySpan<AVPixelFormat> ValidHardwareFormats {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            unsafe {
                return FFHelper.GetSpanFromSentinelTerminatedPtr(
                    Handle.Ref.valid_hw_formats,
                    AVPixelFormat.AV_PIX_FMT_NONE);
            }
        }
    }

    public ReadOnlySpan<AVPixelFormat> ValidSoftwareFormats {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            unsafe {
                return FFHelper.GetSpanFromSentinelTerminatedPtr(
                    Handle.Ref.valid_sw_formats,
                    AVPixelFormat.AV_PIX_FMT_NONE);
            }
        }
    }

    public int MinWidth {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Handle.Ref.min_width;
    }

    public int MinHeight {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Handle.Ref.min_height;
    }

    public int MaxWidth {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Handle.Ref.max_width;
    }

    public int MaxHeight {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Handle.Ref.max_height;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe HardwareFrameConstraints(MediaBuffer<AVHWDeviceContext> deviceCtx, void* hwConfig = null)
    {
        _handle = av_hwdevice_get_hwframe_constraints(deviceCtx.Handle, hwConfig);
        
        if(_handle is null)
            throw new InvalidOperationException("Constraits are not avaliable");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HardwareFrameConstraints(Handle<AVHWFramesConstraints> handle)
    {
        unsafe {
            _handle = handle;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool TryCreate(
        MediaBuffer<AVHWDeviceContext> deviceCtx,
        #if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        [NotNullWhen(true)]
        #endif
        out HardwareFrameConstraints? constraints,
        void* hwConfig = null)
    {
        constraints = null;
        var handle = av_hwdevice_get_hwframe_constraints(deviceCtx.Handle, hwConfig);

        if (handle is null)
            return false;

        constraints = new HardwareFrameConstraints(handle);
        return true;
    }
    
    /// <summary>
    /// Check whenever dimesion are withing range of device constraints
    /// </summary>
    /// <returns>true if is valid</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsValidDimensions(int width, int height)
    {
        return width >= MinWidth && width <= MaxWidth &&
               height >= MinHeight && height <= MaxHeight;
    }
    
    /// <summary>
    /// Check whenever given hardware picture format is supported
    /// </summary>
    /// <returns>true if is valid</returns>
    public bool IsValidHardwareFormat(PictureFormat format)
    {
        if(!IsValidDimensions(format.Width, format.Height))
            return false;

        foreach (var pictureFormat in ValidHardwareFormats) {
            if(pictureFormat == format.PixelFormat)
                return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// Check whenever given software picture format is supported
    /// </summary>
    /// <returns>true if is valid</returns>
    public bool IsValidSoftwareFormat(PictureFormat format)
    {
        if(!IsValidDimensions(format.Width, format.Height))
            return false;

        foreach (var pictureFormat in ValidHardwareFormats) {
            if(pictureFormat == format.PixelFormat)
                return true;
        }
        
        return false;
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected override unsafe void Free()
    {
        fixed (AVHWFramesConstraints** desc = &_handle) {
            av_hwframe_constraints_free(desc);
        }
    }
}
