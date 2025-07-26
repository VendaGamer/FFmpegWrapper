namespace FFmpegWrapper.Codecs;

using System.Runtime.InteropServices;

using Containers;

public readonly struct CodecHardwareConfig : IHandle<AVCodecHWConfig>
{
    public unsafe AVCodecHWConfig* Handle { get; }

    public bool IsValid {
        get {
            unsafe {
                return Handle is not null;
            }
        }
    }
    public readonly MediaCodec Codec;
    public AVHWDeviceType DeviceType {
        get {
            unsafe
            {
                return Handle->device_type;
            }
        }
    }

    public AVPixelFormat PixelFormat {
        get {
            unsafe
            {
                return Handle->pix_fmt;
            }
        }
    }

    public CodecHardwareMethods Methods {
        get {
            unsafe
            {
                return (CodecHardwareMethods)Handle->methods;
            }
        }
    }

    public unsafe CodecHardwareConfig(AVCodec* codec, AVCodecHWConfig* config)
    {
        Codec = new MediaCodec(codec);
        Handle = config;
    }
    public override string ToString() => Codec.Name + " " + PixelFormat.ToString().Substring("AV_PIX_FMT_".Length);
    
    
    /// <summary>
    /// Returns a list of all hardware decoder configurations that may or may not be supported on this machine.
    /// </summary>
    /// <param name="codecId">
    /// If specified, only configurations for that codec will be returned.
    /// </param>
    /// <param name="deviceType">
    /// If specified, only configurations for that device type will be returned.
    /// </param>
    public static IReadOnlyList<CodecHardwareConfig> GetHardwareConfigs(
        AVCodecID? codecId = null,
        AVHWDeviceType? deviceType = null)
    {
        unsafe
        {
            var configs = new List<CodecHardwareConfig>();
            void* iterState = null;
            AVCodec* codec;

            while ((codec = ffmpeg.av_codec_iterate(&iterState)) != null)
            {
                if ((codecId != null && codec->id != codecId) ||
                    ffmpeg.av_codec_is_decoder(codec) == 0)
                    continue;
                
                

                int i = 0;
                AVCodecHWConfig* configPtr;

                while ((configPtr = ffmpeg.avcodec_get_hw_config(codec, i++)) != null)
                {
                    const int reqMethods = (int)(CodecHardwareMethods.DeviceContext | CodecHardwareMethods.FramesContext);

                    if ((configPtr->methods & reqMethods) != 0 && 
                        (deviceType == null || configPtr->device_type == deviceType))
                    {
                        configs.Add(new CodecHardwareConfig(codec, configPtr));
                    }
                }
            }

            return configs;
        }
    }
}
[Flags]
public enum CodecHardwareMethods
{
    /// <summary>
    /// The codec supports this format via the hw_device_ctx interface.
    /// <para/>
    /// When selecting this format, AVCodecContext.hw_device_ctx should
    /// have been set to a device of the specified type before calling
    /// avcodec_open2().
    /// </summary>
    DeviceContext = 0x01,

    /// <summary>
    /// The codec supports this format via the hw_frames_ctx interface.
    /// <para/>
    /// When selecting this format for a decoder,
    /// AVCodecContext.hw_frames_ctx should be set to a suitable frames
    /// context inside the get_format() callback.  The frames context
    /// must have been created on a device of the specified type.
    /// <para/>
    /// When selecting this format for an encoder,
    /// AVCodecContext.hw_frames_ctx should be set to the context which
    /// will be used for the input frames before calling avcodec_open2().
    /// </summary>
    FramesContext = 0x02,

    /// <summary>
    /// The codec supports this format by some internal method.
    /// <para/>
    /// This format can be selected without any additional configuration -
    /// no device or frames context is required.
    /// </summary>
    Internal = 0x04,
    
    /// <summary>
    /// The codec supports this format by some ad-hoc method.
    /// <para/>
    /// Additional settings and/or function calls are required.  See the
    /// codec-specific documentation for details.  (Methods requiring
    /// this sort of configuration are deprecated and others should be
    /// used in preference.)
    /// </summary>
    AdHoc = 0x08,
}