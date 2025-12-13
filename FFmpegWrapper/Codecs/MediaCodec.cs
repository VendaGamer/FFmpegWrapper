namespace FFmpegWrapper.Codecs;

using Configuration;
using Extensions;

public readonly struct MediaCodec : IFFHandleObserver<AVCodec>
{
    public FFHandle<AVCodec> Handle {
        get {
            unsafe {
                return Raw;
            }
        }
    }
    
    public AVCodecID Id {
        get {
            unsafe {
                return Raw->id;
            }
        }
    }

    public AVMediaType Type {
        get {
            unsafe
            {
                return Raw->type;
            }
        }
    }
    
    
    /// <inheritdoc cref="AVCodec.name" />
    public string Name {
        get {
            unsafe
            {
                return FFHelper.PtrToStringUtf8(Raw->name);
            }
        }
    }

    /// <inheritdoc cref="AVCodec.long_name" />
    public string LongName {
        get {
            unsafe
            {
                return FFHelper.PtrToStringUtf8(Raw->long_name)!;
            }
        }
    }

    /// <inheritdoc cref="AVCodec.wrapper_name" />
    public string WrapperName {
        get {
            unsafe
            {
                if (Raw->wrapper_name is null) {
                    return FFHelper.SpanToStringUtf8("builtin"u8);
                }
                return FFHelper.PtrToStringUtf8(Raw->wrapper_name);
            }
        }
    }

    public AVCodecCapabilities Capabilities {
        get {
            unsafe
            {
                return (AVCodecCapabilities)Raw->capabilities;
            }
        }
    }

    /// <inheritdoc cref="AVCodec.max_lowres" />
    public byte MaxLowres {
        get {
            unsafe
            {
                return Raw->max_lowres;
            }
        }
    }
    
    internal readonly unsafe AVCodec* Raw;
    
    public unsafe MediaCodec(FFHandle<AVCodec> raw)
    {
        Raw = raw;
    }
    public static unsafe MediaCodec FromHandle(AVCodec* handle)
    {
        if (handle is null) {
            throw new ArgumentNullException();
        }
        
        return new MediaCodec(handle);
    }

    /// <summary> Array of supported frame rates, or empty if any. </summary>
    public ReadOnlySpan<Rational> SupportedFrameRates
        => GetSupported<Rational>(AVCodecConfig.AV_CODEC_CONFIG_CHANNEL_LAYOUT);

    /// <summary> Array of supported pixel formats, or empty if any. </summary>
    public readonly ReadOnlySpan<AVPixelFormat> SupportedPixelFormats
        => GetSupported<AVPixelFormat>(AVCodecConfig.AV_CODEC_CONFIG_PIX_FORMAT);

    /// <summary> Array of supported audio sample rates, or empty if any. </summary>
    public readonly ReadOnlySpan<int> SupportedSampleRates
        => GetSupported<int>(AVCodecConfig.AV_CODEC_CONFIG_SAMPLE_RATE);

    /// <summary> Array of supported sample formats, or empty if any. </summary>
    public readonly ReadOnlySpan<AVSampleFormat> SupportedSampleFormats
        => GetSupported<AVSampleFormat>(AVCodecConfig.AV_CODEC_CONFIG_SAMPLE_FORMAT);

    /// <summary> Array of supported channel layouts, or empty if any. </summary>
    public readonly ReadOnlySpan<AVChannelLayout> SupportedChannelLayouts
        => GetSupported<AVChannelLayout>(AVCodecConfig.AV_CODEC_CONFIG_PIX_FORMAT);

    private ReadOnlySpan<T> GetSupported<T>(AVCodecConfig config) where T : unmanaged
    {
        unsafe
        {
            T* configs = null;
            int countValue = 0;

            avcodec_get_supported_config(
                null,
                Raw,
                config,
                0,
                (void**)&configs,
                &countValue
            );

            if (countValue > 0) {
                // configsPtr now points to an array of 'countValue' pointers,
                // each one can be cast to AVChannelLayout*
                var layoutSpan = new ReadOnlySpan<T>(
                    configs,
                    countValue
                );
                
                return layoutSpan;
            }
            
            return ReadOnlySpan<T>.Empty;
        }
    }
    
    public bool IsEncoder {
        get {
            unsafe
            {
                return av_codec_is_encoder(Raw) != 0;
            }
        }
    }

    public bool IsDecoder {
        get {
            unsafe
            {
                return av_codec_is_decoder(Raw) != 0;
            }
        }
    }

    /// <summary> Returns a list of options accepted by this codec. </summary>
    public IReadOnlyList<ContextOption> GetOptions(bool removeAliases = true)
    {
        unsafe
        {
            return ContextOption.GetOptions(&Raw->priv_class, removeAliases);
        }
    }

    public static MediaCodec GetEncoder(ReadOnlySpan<byte> name)
    {
        unsafe
        {
            return WrapChecked(avcodec_find_encoder_by_name(name.RawHandle), 0, name);
        }
    }

    public static MediaCodec GetDecoder(ReadOnlySpan<byte> name)
    {
        unsafe
        {
            return WrapChecked(avcodec_find_decoder_by_name(name.RawHandle), 0, name);
        }
    }

    public static MediaCodec GetEncoder(AVCodecID id)
    {
        unsafe
        {
            return WrapChecked(avcodec_find_encoder(id), id);
        }
    }

    public static MediaCodec GetDecoder(AVCodecID id)
    {
        unsafe
        {
            return WrapChecked(avcodec_find_decoder(id), id);
        }
    }

    public static MediaCodec? TryGetEncoder(ReadOnlySpan<byte> name)
    {
        unsafe
        {
            AVCodec* ptr = avcodec_find_encoder_by_name(name.RawHandle);
            return ptr == null ? null : new MediaCodec(ptr);
        }
    }
    public static MediaCodec? TryGetDecoder(ReadOnlySpan<byte> name)
    {
        unsafe
        {
            AVCodec* ptr = avcodec_find_decoder_by_name(name.RawHandle);
            return ptr == null ? null : new MediaCodec(ptr);
        }
    }

    private static unsafe MediaCodec WrapChecked(AVCodec* ptr, AVCodecID id = 0, ReadOnlySpan<byte> name = default)
    {
        if (ptr is not null) {
            return new MediaCodec(ptr);
        }
        
        throw new KeyNotFoundException($"No registered codec named '{name.ToStringUft8()}'");
    }

    public override string ToString() => LongName;

    public static ImmutableArray<MediaCodec> AvailableCodecs
        => Utils.GetAllAvailableCodecs();
    
     
    /// <summary>
    /// Workaround class.
    /// Cannot be directly in MediaCodec struct cause of this issue:
    /// https://github.com/dotnet/runtime/issues/104511
    /// </summary>
    private static class Utils
    {
        private static ImmutableArray<MediaCodec> s_availableCodecs;
        
        public static ImmutableArray<MediaCodec> GetAllAvailableCodecs()
        {
            
            if (!s_availableCodecs.IsDefault) {
                return s_availableCodecs;
            }
            
            var builder = ImmutableArray.CreateBuilder<MediaCodec>(1024);
            
            unsafe {
                void* iterState = null;
                AVCodec* codec;
                while ((codec = av_codec_iterate(&iterState)) != null) {
                    builder.Add(new MediaCodec(codec));
                }
            }
            
            s_availableCodecs =  builder.ToImmutable();
            return s_availableCodecs;
        }
    }
    
}
