namespace FFmpegWrapper.Codecs;

public readonly struct CodecHardwareConfig : IFFHandleObserver<AVCodecHWConfig>
{

    #region Static Methods
    public static ImmutableArray<CodecHardwareConfig> AvailableDecoderConfigs => Utils.GetAvailableDecoderConfigs();
    public static ImmutableArray<CodecHardwareConfig> AvailableEncoderConfigs => Utils.GetAvailableDecoderConfigs();
    private static class Utils
    {
        private static ImmutableArray<CodecHardwareConfig> s_availableDecoderConfigs;
        private static ImmutableArray<CodecHardwareConfig> s_availableEncoderConfigs;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ImmutableArray<CodecHardwareConfig> GetAvailableDecoderConfigs()
        {
            if (s_availableDecoderConfigs.IsDefault) {
                GetAvailableConfigs();
            }

            return s_availableDecoderConfigs;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
                AVCodecHWConfig* res = null;
                foreach (var codec in MediaCodec.AvailableCodecs) {
                    
                    var index = 0;
                    
                    while((res = avcodec_get_hw_config(codec._handle, index)) is not null)
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
    
    public static ReadOnlySpan<CodecHardwareConfig> GetAvailableConfigsFor(FFHandle<AVCodec> codec)
    {
        codec.ThrowIfNull();
        
        unsafe {
            ImmutableArray<CodecHardwareConfig> configs = 
                av_codec_is_decoder(codec) is 0 ?
                Utils.GetAvailableEncoderConfigs() :
                Utils.GetAvailableDecoderConfigs();

            int startIndex = -1;
            
            for (int i = 0; i < configs.Length; i++) {
                if (configs[i].Codec._handle == codec) {
                    startIndex = i;
                    break;
                }
            }
            
            if(startIndex is -1)
                return ReadOnlySpan<CodecHardwareConfig>.Empty;

            int endIndex = startIndex;
            
            for (int i = startIndex+1; i < configs.Length; i++) {
                if (configs[i].Codec._handle != codec) {
                    endIndex = i;
                    break;
                }
            }
            
            return configs.AsSpan(startIndex, endIndex - startIndex);
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
    public readonly MediaCodec Codec;


    public CodecHardwareConfig(FFHandle<AVCodec> handle, FFHandle<AVCodecHWConfig> config)
        : this(new MediaCodec(handle), config) { }
    
    public CodecHardwareConfig(MediaCodec codec, FFHandle<AVCodecHWConfig> config)
    {
        Codec = codec;
        unsafe {
            _handle = config;
        }
    }
    
    public override string ToString()
    {
        return $"{DeviceType} | {PixelFormat} | Methods: {Methods}";
    }
}
