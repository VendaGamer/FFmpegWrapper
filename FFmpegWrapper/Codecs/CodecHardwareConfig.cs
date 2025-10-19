namespace FFmpegWrapper.Codecs;

using System.Net.NetworkInformation;

public readonly struct CodecHardwareConfig : IFFHandleObserver<AVCodecHWConfig>
{

    #region Static Methods

    public static ImmutableArray<CodecHardwareConfig> AvailableDecoderConfigs => Utils.GetAvailableDecoderConfigs();
    public static ImmutableArray<CodecHardwareConfig> AvailableEncoderConfigs => Utils.GetAvailableDecoderConfigs();
    private static class Utils
    {
        private static ImmutableArray<CodecHardwareConfig> s_availableDecoderConfigs;
        private static ImmutableArray<CodecHardwareConfig> s_availableEncoderConfigs;

        public static ImmutableArray<CodecHardwareConfig> GetAvailableDecoderConfigs()
        {
            if (s_availableDecoderConfigs.IsDefault) {
                GetAvailableConfigs();
            }

            return s_availableDecoderConfigs;
        }

        public static ImmutableArray<CodecHardwareConfig> GetAvailableEncoderConfigs()
        {
            if (s_availableEncoderConfigs.IsDefault) {
                GetAvailableConfigs();
            }

            return s_availableEncoderConfigs;
        }
        
        private static void GetAvailableConfigs()
        {
            var encBuilder = ImmutableArray.CreateBuilder<CodecHardwareConfig>();
            var decBuilder = ImmutableArray.CreateBuilder<CodecHardwareConfig>();
            
            unsafe {
                foreach (var codec in MediaCodec.AvailableCodecs) {
                    
                    var index = 0;
                    AVCodecHWConfig* res = null!;
                    
                    while((res = ffmpeg.avcodec_get_hw_config(codec.Raw, index)) != null)
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

            s_availableEncoderConfigs = encBuilder.ToImmutable();
            s_availableDecoderConfigs = decBuilder.ToImmutable();
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
    
    public FFHandle<AVCodecHWConfig> Handle {
        get {
            unsafe
            {
                return _handle;
            }
        }
    }

    public readonly MediaCodec Codec;
    public AVHWDeviceType DeviceType {
        get {
            unsafe
            {
                return _handle->device_type;
            }
        }
    }

    public AVPixelFormat PixelFormat {
        get {
            unsafe
            {
                return _handle->pix_fmt;
            }
        }
    }

    public CodecHardwareMethods Methods {
        get {
            unsafe
            {
                return (CodecHardwareMethods)_handle->methods;
            }
        }
    }
    
    private readonly unsafe AVCodecHWConfig* _handle;

    private unsafe CodecHardwareConfig(AVCodec* codec, AVCodecHWConfig* config)
    {
        Codec = MediaCodec.FromHandle(codec);
        _handle = config;
    }

    private unsafe CodecHardwareConfig(MediaCodec codec, AVCodecHWConfig* config)
    {
        Codec = codec;
        _handle = config;
    }
    public override string ToString()
    {
        return $"{Codec.Name} | {DeviceType} | {PixelFormat} | Methods: {Methods} | {(Codec.IsDecoder ? "Decoder" : "Encoder")}";
    }
}