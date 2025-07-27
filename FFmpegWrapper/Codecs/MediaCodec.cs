namespace FFmpegWrapper.Codecs;

using Configuration;

public readonly struct MediaCodec : IHandle<AVCodec>
{
    public unsafe AVCodec* Handle { get; }
    
    public bool IsValid {
        get {
            unsafe {
                return Handle is not null;
            }
        }
    }

    public AVCodecID Id {
        get {
            unsafe
            {
                return Handle->id;
            }
        }
    }

    public AVMediaType Type {
        get {
            unsafe
            {
                return Handle->type;
            }
        }
    }

    /// <inheritdoc cref="AVCodec.name" />
    public string Name {
        get {
            unsafe
            {
                return Helpers.PtrToStringUTF8(Handle->name)!;
            }
        }
    }

    /// <inheritdoc cref="AVCodec.long_name" />
    public string LongName {
        get {
            unsafe
            {
                return Helpers.PtrToStringUTF8(Handle->long_name)!;
            }
        }
    }

    /// <inheritdoc cref="AVCodec.wrapper_name" />
    public string? WrapperName {
        get {
            unsafe
            {
                return Helpers.PtrToStringUTF8(Handle->wrapper_name);
            }
        }
    }

    public MediaCodecCaps Capabilities {
        get {
            unsafe
            {
                return (MediaCodecCaps)Handle->capabilities;
            }
        }
    }

    /// <inheritdoc cref="AVCodec.max_lowres" />
    public byte MaxLowres {
        get {
            unsafe
            {
                return Handle->max_lowres;
            }
        }
    }

    /// <summary> Span of supported framerates, or empty if any. </summary>
    public ReadOnlySpan<Rational> SupportedFramerates =>
        GetSupportedSpan<Rational>(AVCodecConfig.AV_CODEC_CONFIG_FRAME_RATE);
    /// <summary> Span of supported pixel formats, or empty if unknown. </summary>
    public ReadOnlySpan<AVPixelFormat> SupportedPixelFormats =>
        GetSupportedSpan<AVPixelFormat>(AVCodecConfig.AV_CODEC_CONFIG_PIX_FORMAT);
    /// <summary> Span of supported audio samplerates, or empty if unknown. </summary>
    public ReadOnlySpan<int> SupportedSampleRates =>
        GetSupportedSpan<int>(AVCodecConfig.AV_CODEC_CONFIG_SAMPLE_RATE);
    /// <summary> Span of supported sample formats, or empty if unknown. </summary>
    public ReadOnlySpan<AVSampleFormat> SupportedSampleFormats
        => GetSupportedSpan<AVSampleFormat>(AVCodecConfig.AV_CODEC_CONFIG_SAMPLE_FORMAT);

    /// <summary> Span of supported channel layouts. </summary>
    public ReadOnlySpan<AVChannelLayout> SupportedChannelLayouts
        => GetSupportedSpan<AVChannelLayout>(AVCodecConfig.AV_CODEC_CONFIG_CHANNEL_LAYOUT);

    private ReadOnlySpan<T> GetSupportedSpan<T>(AVCodecConfig config) where T : unmanaged
    {
        unsafe
        {

            T* configs = null;
            int     countValue = 0;
            int*   countAddr   = &countValue;

            int ret = ffmpeg.avcodec_get_supported_config(
                null,
                Handle,
                config,
                0,
                (void**)&configs,
                countAddr
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
                return ffmpeg.av_codec_is_encoder(Handle) != 0;
            }
        }
    }

    public bool IsDecoder {
        get {
            unsafe
            {
                return ffmpeg.av_codec_is_decoder(Handle) != 0;
            }
        }
    }

    public unsafe MediaCodec(AVCodec* handle) => Handle = handle;

    /// <summary> Returns a list of options accepted by this codec. </summary>
    public IReadOnlyList<ContextOption> GetOptions(bool removeAliases = true)
    {
        unsafe
        {
            return ContextOption.GetOptions(&Handle->priv_class, removeAliases);
        }
    }

    public static MediaCodec GetEncoder(string name)
    {
        unsafe
        {
            return WrapChecked(ffmpeg.avcodec_find_encoder_by_name(name), 0, name);
        }
    }

    public static MediaCodec GetDecoder(string name)
    {
        unsafe
        {
            return WrapChecked(ffmpeg.avcodec_find_decoder_by_name(name), 0, name);
        }
    }

    public static MediaCodec GetEncoder(AVCodecID id)
    {
        unsafe
        {
            return WrapChecked(ffmpeg.avcodec_find_encoder(id), id);
        }
    }

    public static MediaCodec GetDecoder(AVCodecID id)
    {
        unsafe
        {
            return WrapChecked(ffmpeg.avcodec_find_decoder(id), id);
        }
    }

    public static MediaCodec? TryGetEncoder(string name)
    {
        unsafe
        {
            AVCodec* ptr = ffmpeg.avcodec_find_encoder_by_name(name);
            return ptr == null ? null : new MediaCodec(ptr);
        }
    }
    public static MediaCodec? TryGetDecoder(string name)
    {
        unsafe
        {
            AVCodec* ptr = ffmpeg.avcodec_find_decoder_by_name(name);
            return ptr == null ? null : new MediaCodec(ptr);
        }
    }

    private static unsafe MediaCodec WrapChecked(AVCodec* ptr, AVCodecID id = 0, string? name = null)
    {
        if (ptr != null) {
            return new MediaCodec(ptr);
        }
        name ??= id.ToString();
        throw new KeyNotFoundException($"No registered codec named '{name}'");
    }

    public override string ToString() => LongName;

    public static ImmutableArray<MediaCodec> AvaliableCodecs {
        get {
            if (Utils.avaliableCodecs.IsDefault) {
                Utils.avaliableCodecs = GetAllAvailableCodecs();
            }

            return Utils.avaliableCodecs;
        }
    }

    private static ImmutableArray<MediaCodec> GetAllAvailableCodecs()
    {
        var builder = ImmutableArray.CreateBuilder<MediaCodec>(768);
        
        unsafe {
            void* iterState = null;
            AVCodec* codec;
            while ((codec = ffmpeg.av_codec_iterate(&iterState)) != null) {
                builder.Add(new MediaCodec(codec));
            }
        }

        return builder.ToImmutable();
    }
    
    
    /// <summary>
    /// Workaround class.
    /// Cannot be directly in MediaCodec struct cause of this issue:
    /// https://github.com/dotnet/runtime/issues/104511
    /// </summary>
    private static class Utils
    {
        public static ImmutableArray<MediaCodec> avaliableCodecs = default;
    }
    
}