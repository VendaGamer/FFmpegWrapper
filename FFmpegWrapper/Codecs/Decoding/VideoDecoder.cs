namespace FFmpegWrapper.Codecs.Decoding;

using System.Runtime.InteropServices;
using Extensions;
using Hardware;
using Media;

public class VideoDecoder : MediaDecoder
{
    public PictureFormat FrameFormat {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            ref var handle = ref Handle.Ref;
            
            return new PictureFormat(
                handle.width,
                handle.height,
                handle.pix_fmt,
                handle.sample_aspect_ratio
                );
        }
    }
    
    /// <summary>
    /// Unaccelerated format
    /// </summary>
    public AVPixelFormat SoftwarePixelFormat {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Handle.Ref.sw_pix_fmt;
    }

    public PictureColorspace Colorspace {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            ref var handle = ref Handle.Ref;
            
            return new PictureColorspace(
                handle.colorspace,
                handle.color_primaries,
                handle.color_trc,
                handle.color_range,
                handle.chroma_sample_location);
        }
    }

    public VideoDecoder(NullableHandle<AVCodec> ctx = default) : base(ctx)
    {
        
    }

    public VideoDecoder(AVCodecID id) : base(id)
    {
        
    }

    public VideoDecoder(Handle<AVCodecContext> handle) : base(handle)
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
        NullableHandle<AVBufferRef> hwFramePool = default)
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
        Handle<AVCodecContext> ctx,
        ReadOnlySpan<AVPixelFormat> pixelFormats)
    {
        unsafe
        {
            return avcodec_default_get_format(ctx, pixelFormats.RawHandle);
        }
    }
}