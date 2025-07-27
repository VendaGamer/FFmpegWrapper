namespace FFmpegWrapper.Hardware;

public class HardwareFrameConstraints : FFObject<AVHWFramesConstraints>
{
    public readonly ImmutableArray<AVPixelFormat> ValidHardwareFormats;
    public readonly ImmutableArray<AVPixelFormat> ValidSoftwareFormats;

    public int MinWidth {
        get {
            unsafe {
                ThrowIfDisposed();
                return handle->min_width;
            }
        }
    }

    public int MinHeight {
        get {
            unsafe {
                ThrowIfDisposed();
                return handle->min_height;
            }
        }
    }

    public int MaxWidth {
        get {
            unsafe {
                ThrowIfDisposed();
                return handle->max_width;
            }
        }
    }

    public int MaxHeight {
        get {
            unsafe {
                ThrowIfDisposed();
                return handle->max_height;
            }
        }
    }

    public unsafe HardwareFrameConstraints(AVHWFramesConstraints* desc)
    {
        handle = desc;
        
        ValidHardwareFormats = ImmutableArray.Create(
            Helpers.GetSpanFromSentinelTerminatedPtr(desc->valid_hw_formats, PixelFormats.None));
        ValidSoftwareFormats = ImmutableArray.Create(
            Helpers.GetSpanFromSentinelTerminatedPtr(desc->valid_sw_formats, PixelFormats.None));
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
            fixed (AVHWFramesConstraints** desc = &handle) {
                ffmpeg.av_hwframe_constraints_free(desc);
            }
        }
    }
}