namespace FFmpegWrapper.Codecs;

using Configuration;

using Extensions;

public readonly struct MediaCodec : IHandle<AVCodec>
{
    internal readonly unsafe AVCodec* handle;
    unsafe AVCodec* IHandle<AVCodec>.Handle => handle;
    
    public bool IsValid {
        get {
            unsafe {
                return handle is not null;
            }
        }
    }

    public AVCodecID Id {
        get {
            unsafe
            {
                return handle->id;
            }
        }
    }

    public AVMediaType Type {
        get {
            unsafe
            {
                return handle->type;
            }
        }
    }

    /// <inheritdoc cref="AVCodec.name" />
    public string Name {
        get {
            unsafe
            {
                return Helpers.PtrToStringUTF8(handle->name)!;
            }
        }
    }

    /// <inheritdoc cref="AVCodec.long_name" />
    public string LongName {
        get {
            unsafe
            {
                return Helpers.PtrToStringUTF8(handle->long_name)!;
            }
        }
    }

    /// <inheritdoc cref="AVCodec.wrapper_name" />
    public string WrapperName {
        get {
            unsafe
            {
                if (handle->wrapper_name is null) {
                    return Helpers.SpanToStringUTF8("builtin"u8);
                }
                return Helpers.PtrToStringUTF8(handle->wrapper_name);
            }
        }
    }

    public MediaCodecCaps Capabilities {
        get {
            unsafe
            {
                return (MediaCodecCaps)handle->capabilities;
            }
        }
    }

    /// <inheritdoc cref="AVCodec.max_lowres" />
    public byte MaxLowres {
        get {
            unsafe
            {
                return handle->max_lowres;
            }
        }
    }
    
    private unsafe MediaCodec(AVCodec* handle)
    {
        this.handle = handle;

        SupportedChannelLayouts =
            GetSupported<AVChannelLayout>(AVCodecConfig.AV_CODEC_CONFIG_CHANNEL_LAYOUT);
        SupportedFramerates =
            GetSupported<Rational>(AVCodecConfig.AV_CODEC_CONFIG_FRAME_RATE);
        SupportedPixelFormats =
            GetSupported<AVPixelFormat>(AVCodecConfig.AV_CODEC_CONFIG_PIX_FORMAT);
        SupportedSampleRates =
            GetSupported<int>(AVCodecConfig.AV_CODEC_CONFIG_SAMPLE_RATE);
        SupportedSampleFormats = 
            GetSupported<AVSampleFormat>(AVCodecConfig.AV_CODEC_CONFIG_SAMPLE_FORMAT);
    }

    public static MediaCodec FromHandle(IHandle<AVCodec> handle)
    {
        unsafe
        {
            if (handle is null) {
                throw new ArgumentNullException();
            }

            return new MediaCodec(handle.Handle);
        }
    }

    public static unsafe MediaCodec FromHandle(AVCodec* handle)
    {
        if (handle is null) {
            throw new ArgumentNullException();
        }
        
        return new MediaCodec(handle);
    }

    /// <summary> Array of supported framerates, or empty if any. </summary>
    public readonly ImmutableArray<Rational> SupportedFramerates;

    /// <summary> Array of supported pixel formats, or empty if unknown. </summary>
    public readonly ImmutableArray<AVPixelFormat> SupportedPixelFormats;

    /// <summary> Array of supported audio samplerates, or empty if unknown. </summary>
    public readonly ImmutableArray<int> SupportedSampleRates;

    /// <summary> Array of supported sample formats, or empty if unknown. </summary>
    public readonly ImmutableArray<AVSampleFormat> SupportedSampleFormats;

    /// <summary> Array of supported channel layouts. </summary>
    public readonly ImmutableArray<AVChannelLayout> SupportedChannelLayouts;

    private ImmutableArray<T> GetSupported<T>(AVCodecConfig config) where T : unmanaged
    {
        unsafe
        {
            T* configs = null;
            int     countValue = 0;
            int*   countAddr   = &countValue;

            int ret = ffmpeg.avcodec_get_supported_config(
                null,
                handle,
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
                
                return ImmutableArray.Create(layoutSpan);
            }
            
            return ImmutableArray<T>.Empty;
        }
    }
    
    public bool IsEncoder {
        get {
            unsafe
            {
                return ffmpeg.av_codec_is_encoder(handle) != 0;
            }
        }
    }

    public bool IsDecoder {
        get {
            unsafe
            {
                return ffmpeg.av_codec_is_decoder(handle) != 0;
            }
        }
    }

    /// <summary> Returns a list of options accepted by this codec. </summary>
    public IReadOnlyList<ContextOption> GetOptions(bool removeAliases = true)
    {
        unsafe
        {
            return ContextOption.GetOptions(&handle->priv_class, removeAliases);
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

    public static ImmutableArray<MediaCodec> AvaliableCodecs
        => Utils.GetAllAvailableCodecs();
    
    
    /// <summary>
    /// Workaround class.
    /// Cannot be directly in MediaCodec struct cause of this issue:
    /// https://github.com/dotnet/runtime/issues/104511
    /// </summary>
    private static class Utils
    {
        private static ImmutableArray<MediaCodec> avaliableCodecs;
        
        public static ImmutableArray<MediaCodec> GetAllAvailableCodecs()
        {
            
            if (!avaliableCodecs.IsDefault) {
                return avaliableCodecs;
            }
            
            var builder = ImmutableArray.CreateBuilder<MediaCodec>(768);
            
            unsafe {
                void* iterState = null;
                AVCodec* codec;
                while ((codec = ffmpeg.av_codec_iterate(&iterState)) != null) {
                    builder.Add(new MediaCodec(codec));
                }
            }

            avaliableCodecs =  builder.ToImmutable();
            return avaliableCodecs;
        }
    }
    
}