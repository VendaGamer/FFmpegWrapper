namespace FFmpegWrapper.Codecs;

public readonly struct CodecHardwareConfig : IFFHandle<AVCodecHWConfig>
{

    #region Static Methods

    public static ImmutableArray<CodecHardwareConfig> AvaliableDecoderConfigs => Utils.GetAvaliableDecoderConfigs();
    public static ImmutableArray<CodecHardwareConfig> AvaliableEncoderConfigs => Utils.GetAvaliableDecoderConfigs();
    private static class Utils
    {
        private static ImmutableArray<CodecHardwareConfig> avaliableDecoderConfigs;
        private static ImmutableArray<CodecHardwareConfig> avaliableEncoderConfigs;

        public static ImmutableArray<CodecHardwareConfig> GetAvaliableDecoderConfigs()
        {
            if (avaliableDecoderConfigs.IsDefault) {
                GetAvaliableConfigs();
            }

            return avaliableDecoderConfigs;
        }

        public static ImmutableArray<CodecHardwareConfig> GetAvaliableEncoderConfigs()
        {
            if (avaliableEncoderConfigs.IsDefault) {
                GetAvaliableConfigs();
            }

            return avaliableEncoderConfigs;
        }
        
        private static void GetAvaliableConfigs()
        {
            var encBuilder = ImmutableArray.CreateBuilder<CodecHardwareConfig>();
            var decBuilder = ImmutableArray.CreateBuilder<CodecHardwareConfig>();
            
            unsafe {
                foreach (var codec in MediaCodec.AvailableCodecs) {
                    
                    var index = 0;
                    AVCodecHWConfig* res = null!;
                    
                    while((res = ffmpeg.avcodec_get_hw_config(codec._handle, index)) != null)
                    {
                        if (codec.IsDecoder) {
                            decBuilder.Add(new CodecHardwareConfig(codec, res));
                        } else {
                            encBuilder.Add(new CodecHardwareConfig(codec, res));
                        }

                        index++;
                    }
                }
            }

            avaliableEncoderConfigs = encBuilder.ToImmutable();
            avaliableDecoderConfigs = decBuilder.ToImmutable();
        }
    }
    

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
        unsafe {
            var configs = new List<CodecHardwareConfig>();
            void* iterState = null;
            AVCodec* codec;

            while ((codec = ffmpeg.av_codec_iterate(&iterState)) != null) {
                if ((codecId != null && codec->id != codecId) ||
                    ffmpeg.av_codec_is_decoder(codec) == 0)
                    continue;



                int i = 0;
                AVCodecHWConfig* configPtr;

                while ((configPtr = ffmpeg.avcodec_get_hw_config(codec, i++)) != null) {
                    const int reqMethods =
                        (int)(CodecHardwareMethods.DeviceContext | CodecHardwareMethods.FramesContext);

                    if ((configPtr->methods & reqMethods) != 0 &&
                        (deviceType == null || configPtr->device_type == deviceType)) {
                        configs.Add(new CodecHardwareConfig(codec, configPtr));
                    }
                }
            }

            return configs;
        }
    }

    #endregion

    private readonly unsafe AVCodecHWConfig* handle;
    unsafe AVCodecHWConfig* IFFHandle<AVCodecHWConfig>.Handle => handle;

    public readonly MediaCodec Codec;
    public AVHWDeviceType DeviceType {
        get {
            unsafe
            {
                return handle->device_type;
            }
        }
    }

    public AVPixelFormat PixelFormat {
        get {
            unsafe
            {
                return handle->pix_fmt;
            }
        }
    }

    public CodecHardwareMethods Methods {
        get {
            unsafe
            {
                return (CodecHardwareMethods)handle->methods;
            }
        }
    }

    private unsafe CodecHardwareConfig(AVCodec* codec, AVCodecHWConfig* config)
    {
        Codec = MediaCodec.FromHandle(codec);
        handle = config;
    }

    private unsafe CodecHardwareConfig(MediaCodec codec, AVCodecHWConfig* config)
    {
        Codec = codec;
        handle = config;
    }
    public override string ToString() 
    {
        var deviceTypeName = DeviceType.ToString().Replace("AV_HWDEVICE_TYPE_", "");
        var pixelFormatName = PixelFormat.ToString().StartsWith("AV_PIX_FMT_") 
            ? PixelFormat.ToString().Substring(11) 
            : PixelFormat.ToString();
    
        var methodsStr = Methods.ToString().Replace("CodecHardwareMethods.", "");
    
        return $"{Codec.Name} | {deviceTypeName} | {pixelFormatName} | Methods: {methodsStr} | {(Codec.IsDecoder ? "Decoder" : "Encoder")}";
    }
}