namespace FFmpegWrapper.Codecs;

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