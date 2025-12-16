namespace FFmpegWrapper.Codecs.Decoding;

using System.Runtime.InteropServices;

using Hardware;
using Media;

public class VideoDecoder(MediaCodec codec) : MediaDecoder(AllocContext(codec))
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

    public VideoDecoder(AVCodecID codecId)
        : this(MediaCodec.GetDecoder(codecId))
    {
        
    }

    AVCodecContext.AVCodecContext_get_format? _chooseHwPixelFmt;

    /// <summary>
    /// Before the decoder is open, setups hardware acceleration via the specified device. 
    /// If the device does not support the input format, a software decoder will be used instead.
    /// </summary>
    public void SetupHardwareAccelerator(CodecHardwareConfig config, HardwareDevice device)
    {
        unsafe
        {
            ThrowIfOpen();
            ThrowIfDisposed();
        
            SetHardwareContext(config, device, null);
            //TODO: support custom decoder negotiation and hw_frames_ctx
 
            _chooseHwPixelFmt = (ctx, pAvailFmts) => {
                for (var pFmt = pAvailFmts; *pFmt is not AVPixelFormat.AV_PIX_FMT_NONE; pFmt++) {
                    if (*pFmt == config.PixelFormat) {
                        return *pFmt;
                    }
                }
                return ctx->sw_pix_fmt;
            };

            _handle->get_format = (delegate* unmanaged[Cdecl]<AVCodecContext*, AVPixelFormat*, AVPixelFormat>)
                Marshal.GetFunctionPointerForDelegate(_chooseHwPixelFmt);
        }
    }
    
}
