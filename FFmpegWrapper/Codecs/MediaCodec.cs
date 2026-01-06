namespace FFmpegWrapper.Codecs;

using Configuration;
using Extensions;

public readonly struct MediaCodec : IFFHandleObserver<AVCodec>
{

    #region Static Properties

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

    #endregion
    
    #region Properties

    public FFHandle<AVCodec> Handle {
        get {
            unsafe {
                return _handle;
            }
        }
    }
    
    public AVCodecID Id {
        get {
            unsafe {
                return _handle->id;
            }
        }
    }

    public AVMediaType Type {
        get {
            unsafe
            {
                return _handle->type;
            }
        }
    }
    
    
    /// <inheritdoc cref="AVCodec.name" />
    public string Name {
        get {
            unsafe
            {
                return FFHelper.PtrToStringUtf8(_handle->name);
            }
        }
    }

    /// <inheritdoc cref="AVCodec.long_name" />
    public string LongName {
        get {
            unsafe
            {
                return FFHelper.PtrToStringUtf8(_handle->long_name)!;
            }
        }
    }

    /// <inheritdoc cref="AVCodec.wrapper_name" />
    public string WrapperName {
        get {
            unsafe
            {
                if (_handle->wrapper_name is null) {
                    return FFHelper.SpanToStringUtf8("builtin"u8);
                }
                return FFHelper.PtrToStringUtf8(_handle->wrapper_name);
            }
        }
    }

    public AVCodecCapabilities Capabilities {
        get {
            unsafe
            {
                return (AVCodecCapabilities)_handle->capabilities;
            }
        }
    }

    /// <inheritdoc cref="AVCodec.max_lowres" />
    public byte MaxLowres {
        get {
            unsafe
            {
                return _handle->max_lowres;
            }
        }
    }

    #endregion

    #region Fields

    internal readonly unsafe AVCodec* _handle;

    #endregion
    
    
    
    public unsafe MediaCodec(FFHandle<AVCodec> handle)
    {
        _handle = handle;
    }

    /// <summary> Span of supported frame rates, or empty if any. </summary>
    public ReadOnlySpan<Rational> SupportedFrameRates
        => GetSupported<Rational>(AVCodecConfig.AV_CODEC_CONFIG_CHANNEL_LAYOUT);

    /// <summary> Span of supported pixel formats, or empty if any. </summary>
    public readonly ReadOnlySpan<AVPixelFormat> SupportedPixelFormats
        => GetSupported<AVPixelFormat>(AVCodecConfig.AV_CODEC_CONFIG_PIX_FORMAT);

    /// <summary> Span of supported audio sample rates, or empty if any. </summary>
    public readonly ReadOnlySpan<int> SupportedSampleRates
        => GetSupported<int>(AVCodecConfig.AV_CODEC_CONFIG_SAMPLE_RATE);

    /// <summary> Span of supported sample formats, or empty if any. </summary>
    public readonly ReadOnlySpan<AVSampleFormat> SupportedSampleFormats
        => GetSupported<AVSampleFormat>(AVCodecConfig.AV_CODEC_CONFIG_SAMPLE_FORMAT);

    /// <summary> Span of supported channel layouts, or empty if any. </summary>
    public readonly ReadOnlySpan<AVChannelLayout> SupportedChannelLayouts
        => GetSupported<AVChannelLayout>(AVCodecConfig.AV_CODEC_CONFIG_CHANNEL_LAYOUT);

    private ReadOnlySpan<T> GetSupported<T>(AVCodecConfig config) where T : unmanaged
    {
        unsafe
        {
            T* configs = null;
            int countValue = 0;

            avcodec_get_supported_config(
                null,
                _handle,
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

    public unsafe AVPixelFormat GetBestPixelFormat(AVPixelFormat sourcePixelFormat)
    {
        var desc = new PixelFormatDescriptor(sourcePixelFormat);
        int hasAlpha = (int)(desc.Flags & AV_PIX_FMT_FLAGS.AV_PIX_FMT_FLAG_ALPHA);
        int loss = 0;

        return avcodec_find_best_pix_fmt_of_list(SupportedPixelFormats.RawHandle, sourcePixelFormat, hasAlpha, &loss);
    }
    
    public bool IsEncoder {
        get {
            unsafe
            {
                return av_codec_is_encoder(_handle) != 0;
            }
        }
    }

    public bool IsDecoder {
        get {
            unsafe
            {
                return av_codec_is_decoder(_handle) != 0;
            }
        }
    }

    /// <summary> Returns a list of options accepted by this codec. </summary>
    public IReadOnlyList<ContextOption> GetOptions(bool removeAliases = true)
    {
        unsafe
        {
            return ContextOption.GetOptions(&_handle->priv_class, removeAliases);
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryGetEncoder(ReadOnlySpan<byte> name, out MediaCodec codec)
    {
        unsafe
        {
            NullableFFHandle<AVCodec> handle = avcodec_find_encoder_by_name(name.RawHandle);

            if (handle.IsNull) {
                codec = default;
                return false;
            }

            codec = new MediaCodec(handle.Handle);
            return true;
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryGetDecoder(ReadOnlySpan<byte> name, out MediaCodec codec)
    {
        unsafe
        {
            NullableFFHandle<AVCodec> handle = avcodec_find_decoder_by_name(name.RawHandle);
            
            if (handle.IsNull) {
                codec = default;
                return false;
            }

            codec = new MediaCodec(handle.Handle);
            return true;
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
}
