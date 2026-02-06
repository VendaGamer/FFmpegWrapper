namespace FFmpegWrapper.Hardware;


public class HardwareFrameConstraints : OwnedObject<AVHWFramesConstraints>
{
    public readonly ImmutableArray<AVPixelFormat> ValidHardwareFormats;
    public readonly ImmutableArray<AVPixelFormat> ValidSoftwareFormats;

    public int MinWidth {
        get {
            unsafe {
                ThrowIfDisposed();
                return _handle->min_width;
            }
        }
    }

    public int MinHeight {
        get {
            unsafe {
                ThrowIfDisposed();
                return _handle->min_height;
            }
        }
    }

    public int MaxWidth {
        get {
            unsafe {
                ThrowIfDisposed();
                return _handle->max_width;
            }
        }
    }

    public int MaxHeight {
        get {
            unsafe {
                ThrowIfDisposed();
                return _handle->max_height;
            }
        }
    }

    public unsafe HardwareFrameConstraints(AVHWFramesConstraints* desc)
    {
        _handle = desc;
        
        ValidHardwareFormats = ImmutableArray.Create(
            FFHelper.GetSpanFromSentinelTerminatedPtr(desc->valid_hw_formats, AVPixelFormat.AV_PIX_FMT_NONE));
        ValidSoftwareFormats = ImmutableArray.Create(
            FFHelper.GetSpanFromSentinelTerminatedPtr(desc->valid_sw_formats, AVPixelFormat.AV_PIX_FMT_NONE));
    }
    /// <summary>
    /// Check whenever dimesion are withing range of device constraints
    /// </summary>
    /// <returns>true if is valid</returns>
    public bool IsValidDimensions(int width, int height)
    {
        return width >= MinWidth && width <= MaxWidth &&
               height >= MinHeight && height <= MaxHeight;
    }
    
    /// <summary>
    /// Check whenever given format is supported
    /// </summary>
    /// <returns>true if is valid</returns>
    public bool IsValidFormat(in PictureFormat format)
    {
        return IsValidDimensions(format.Width, format.Height) && 
               ValidSoftwareFormats.IndexOf(format.PixelFormat) >= 0;
    }

    /// <inheritdoc />
    protected override void Free()
    {
        unsafe {
            fixed (AVHWFramesConstraints** desc = &_handle) {
                av_hwframe_constraints_free(desc);
            }
        }
    }
}
