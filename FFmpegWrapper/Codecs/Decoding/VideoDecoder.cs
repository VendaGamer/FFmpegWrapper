namespace FFmpegWrapper.Codecs.Decoding;

using System.Runtime.InteropServices;

using Extensions;

using Hardware;
using Media;

public class VideoDecoder : MediaDecoder
{
    public int Width {
        get {
            unsafe
            {
                return Handle.Raw->width;
            }
        }
    }

    public int Height {
        get {
            unsafe
            {
                return Handle.Raw->height;
            }
        }
    }

    public AVPixelFormat PixelFormat {
        get {
            unsafe
            {
                return Handle.Raw->pix_fmt;
            }
        }
    }

    public PictureFormat FrameFormat {
        get {
            unsafe
            {
                return new PictureFormat(Width, Height, PixelFormat, Handle.Raw->sample_aspect_ratio);
            }
        }
    }

    public PictureColorspace Colorspace {
        get {
            unsafe
            {
                ThrowIfDisposed();
            
                return new PictureColorspace(_handle->colorspace, _handle->color_primaries,
                    _handle->color_trc, _handle->color_range);
            }
        }
    }

    public VideoDecoder(NullableFFHandle<AVCodec> ctx = default) : base(ctx)
    {
        
    }

    public VideoDecoder(AVCodecID id) : base(id)
    {
        
    }

    public VideoDecoder(FFHandle<AVCodecContext> handle) : base(handle)
    {
        unsafe {
            _handle->get_format =
                (delegate* unmanaged[Cdecl]<AVCodecContext*, AVPixelFormat*, AVPixelFormat>)
                Marshal.GetFunctionPointerForDelegate(GetFormatCore);
        }
    }
    
    /// <summary>
    /// Before the decoder is open, setups hardware acceleration via the specified device. 
    /// If the device does not support the input format, a software decoder will be used instead.
    /// </summary>
    public void SetupHardwareAccelerator(
        CodecHardwareConfig config,
        HardwareDevice device,
        NullableFFHandle<AVBufferRef> hwFramePool = default)
    {
        ThrowIfOpen();
        ThrowIfDisposed();
        
        SetHardwareContext(config, device, null);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe AVPixelFormat GetFormatCore(
        AVCodecContext* ctx,
        AVPixelFormat* formats) => GetFormat(ctx,
        FFHelper.GetSpanFromSentinelTerminatedPtr(formats,
            AVPixelFormat.AV_PIX_FMT_NONE));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected virtual AVPixelFormat GetFormat(
        FFHandle<AVCodecContext> ctx,
        ReadOnlySpan<AVPixelFormat> pixelFormats)
    {
        unsafe
        {
            return avcodec_default_get_format(ctx, pixelFormats.RawHandle);
        }
    }
}