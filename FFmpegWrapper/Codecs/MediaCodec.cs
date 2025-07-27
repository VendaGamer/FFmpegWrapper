namespace FFmpegWrapper.Codecs;

using System.Collections.Immutable;
using Configuration;
using Containers;
using Core;

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
[Flags]
public enum MediaCodecCaps
{
    /// <summary> Decoder can use draw_horiz_band callback. </summary>
    DrawHorizontalBand = ffmpeg.AV_CODEC_CAP_DRAW_HORIZ_BAND,
    /// <summary> 
    /// Codec uses get_buffer() or get_encode_buffer() for allocating buffers and
    /// supports custom allocators.
    /// If not set, it might not use get_buffer() or get_encode_buffer() at all, or
    /// use operations that assume the buffer was allocated by
    /// avcodec_default_get_buffer2 or avcodec_default_get_encode_buffer.
    /// </summary>
    DR1 = ffmpeg.AV_CODEC_CAP_DR1,
    /// <summary> 
    /// Encoder or decoder requires flushing with NULL input at the end in order to
    /// give the complete and correct output.
    /// </summary>
    /// <remarks>
    /// If this flag is not set, the codec is guaranteed to never be fed with
    /// with NULL data. The user can still send NULL data to the public encode
    /// or decode function, but libavcodec will not pass it along to the codec
    /// unless this flag is set.
    /// <para/>
    /// Decoders:
    /// The decoder has a non-zero delay and needs to be fed with avpkt->data=NULL,
    /// avpkt->size=0 at the end to get the delayed data until the decoder no longer
    /// returns frames.
    /// <para/>
    /// Encoders:
    /// The encoder needs to be fed with NULL data at the end of encoding until the
    /// encoder no longer returns data.
    /// <para/>
    /// For encoders implementing the AVCodec.encode2() function, setting this
    /// flag also means that the encoder must set the pts and duration for
    /// each output packet. If this flag is not set, the pts and duration will
    /// be determined by libavcodec from the input frame.
    /// </remarks>
    Delay = ffmpeg.AV_CODEC_CAP_DELAY,
    /// <summary> 
    /// Codec can be fed a final frame with a smaller size.
    /// This can be used to prevent truncation of the last audio samples.
    /// </summary>
    SmallLastFrame = ffmpeg.AV_CODEC_CAP_SMALL_LAST_FRAME,

    /// <summary> 
    /// Codec can output multiple frames per AVPacket.
    /// Normally demuxers return one frame at a time, demuxers which do not do
    /// are connected to a parser to split what they return into proper frames.
    /// This flag is reserved to the very rare category of codecs which have a
    /// bitstream that cannot be split into frames without timeconsuming
    /// operations like full decoding. Demuxers carrying such bitstreams thus
    /// may return multiple frames in a packet. This has many disadvantages like
    /// prohibiting stream copy in many cases thus it should only be considered
    /// as a last resort.
    /// </summary>
    SubFrames = ffmpeg.AV_CODEC_CAP_SUBFRAMES,

    /// <summary> Codec is experimental and is thus avoided in favor of non experimental encoders, </summary>
    Experimental = ffmpeg.AV_CODEC_CAP_EXPERIMENTAL,
    /// <summary>  Codec should fill in channel configuration and samplerate instead of container, </summary>
    ChannelConfig = ffmpeg.AV_CODEC_CAP_CHANNEL_CONF,
    /// <summary>  Codec supports frame-level multithreading. </summary>
    FrameThreads = ffmpeg.AV_CODEC_CAP_FRAME_THREADS,
    /// <summary> Codec supports slice-based (or partition-based) multithreading. </summary>
    SliceThreads = ffmpeg.AV_CODEC_CAP_SLICE_THREADS,
    /// <summary> Codec supports changed parameters at any point. </summary>
    ParamChange = ffmpeg.AV_CODEC_CAP_PARAM_CHANGE,
    /// <summary> 
    /// Codec supports multithreading through a method other than slice- or
    /// frame-level multithreading. Typically this marks wrappers around
    /// multithreading-capable external libraries.
    /// </summary>
    OtherThreads = ffmpeg.AV_CODEC_CAP_OTHER_THREADS,
    /// <summary> Audio encoder supports receiving a different number of samples in each call. </summary>
    VariableFrameSize = ffmpeg.AV_CODEC_CAP_VARIABLE_FRAME_SIZE,
    /// <summary> 
    /// Decoder is not a preferred choice for probing.
    /// This indicates that the decoder is not a good choice for probing.
    /// It could for example be an expensive to spin up hardware decoder,
    /// or it could simply not provide a lot of useful information about
    /// the stream.
    /// A decoder marked with this flag should only be used as last resort
    /// choice for probing.
    /// </summary>
    AvoidProbing = ffmpeg.AV_CODEC_CAP_AVOID_PROBING,
    /// <summary> 
    /// Codec is backed by a hardware implementation. Typically used to
    /// identify a non-hwaccel hardware decoder. For information about hwaccels, use
    /// avcodec_get_hw_config() instead.
    /// </summary>
    Hardware = ffmpeg.AV_CODEC_CAP_HARDWARE,
    /// <summary> 
    /// Codec is potentially backed by a hardware implementation, but not
    /// necessarily. This is used instead of AV_CODEC_CAP_HARDWARE, if the
    /// implementation provides some sort of internal fallback.
    /// </summary>
    Hybrid = ffmpeg.AV_CODEC_CAP_HYBRID,
    /// <summary> 
    /// This encoder can reorder user opaque values from input AVFrames and return
    /// them with corresponding output packets.
    /// @see AV_CODEC_FLAG_COPY_OPAQUE
    /// </summary>
    EncoderReorderedOpaque = ffmpeg.AV_CODEC_CAP_ENCODER_REORDERED_OPAQUE,
    /// <summary> 
    /// This encoder can be flushed using avcodec_flush_buffers(). If this flag is
    /// not set, the encoder must be closed and reopened to ensure that no frames
    /// remain pending.
    /// </summary>
    EncoderFlush = ffmpeg.AV_CODEC_CAP_ENCODER_FLUSH,
    /// <summary> 
    /// The encoder is able to output reconstructed frame data, i.e. raw frames that
    /// would be produced by decoding the encoded bitstream.
    ///
    /// Reconstructed frame output is enabled by the AV_CODEC_FLAG_RECON_FRAME flag.
    /// </summary>
    EncoderReconstructedFrame = ffmpeg.AV_CODEC_CAP_ENCODER_RECON_FRAME
}