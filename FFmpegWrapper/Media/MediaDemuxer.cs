namespace FFmpegWrapper.Media;

using Abstractions;
using Codecs;
using Codecs.Decoding;
using Extensions;
using Streams;

public class MediaDemuxer : FFObject<AVFormatContext>
{
    
    #region Properties
    
    public TimeSpan? Duration => FFHelper.GetTimeSpan(Handle.Ref.duration, new Rational(1, AV_TIME_BASE));

    /// <summary> An array of all streams in the file. </summary>
    public ReadOnlySpan<MediaStream> Streams {
        get {
            unsafe
            {
                return new ReadOnlySpan<MediaStream>(Handle.Ref.streams, (int)Handle.Ref.nb_streams);
            }
        }
    }

    /// <inheritdoc cref="AVFormatContext.metadata" />
    public readonly MediaDictionaryOwner Metadata;

    public bool CanSeek {
        get {
            unsafe
            {
                return Handle.Ref.pb->seekable is not 0;
            }
        }
    }
    
    #endregion
    
    
    private readonly IFFHandleOwner<AVIOContext>? _ioContext;
    
    /// <summary>
    /// Opens an existing resource URL for demuxing.
    /// </summary>
    /// <param name="url">Media source URL</param>
    public MediaDemuxer(ReadOnlySpan<byte> url) : this(CreateContext(url)) { }

    /// <inheritdoc />
    public MediaDemuxer(IFFHandleOwner<AVIOContext> inputOutputContextOwner)
        : this(CreateContext(pb: inputOutputContextOwner.Handle)) 
    {
        _ioContext = inputOutputContextOwner;
    }

    public MediaDemuxer(FFHandle<AVIOContext> inputOutputContext)
        : this(CreateContext(pb: inputOutputContext)) { }

    /// <summary> Opens an existing resource URL for demuxing. </summary>
    /// <remarks> See https://ffmpeg.org/ffmpeg-formats.html, https://ffmpeg.org/ffmpeg-protocols.html </remarks>
    /// <param name="url">URL to be opened for demuxing</param>
    /// <param name="options"> A dictionary filled with AVFormatContext and demuxer-private options. </param>
    public MediaDemuxer(ReadOnlySpan<byte> url, ReadOnlySpan<Utf8KeyValue> options)
        : this(CreateContext(options, url, null)) { }

    /// <summary> Wraps a pointer to an open <see cref="AVFormatContext"/>. </summary>
    /// <param name="ctx"></param>
    public MediaDemuxer(FFHandle<AVFormatContext> ctx)
    {
        unsafe
        {
            _handle = ctx;
            Metadata = new MediaDictionaryOwner(_handle->metadata);
        }
    }

    private static unsafe FFHandle<AVFormatContext> CreateContext(
        ReadOnlySpan<Utf8KeyValue> options,
        ReadOnlySpan<byte> url = default,
        FFHandle<AVIOContext> pb = default)
    {
        if (options.IsEmpty)
            return CreateContext(url, pb);
        
        return CreateContext(url, pb, MediaDictionaryOwner.CreateFromEntries(options).Handle);
    }
    
    private static unsafe FFHandle<AVFormatContext> CreateContext(
        ReadOnlySpan<byte> url = default,
        FFHandle<AVIOContext> pb = default,
        NullableFFHandle<AVDictionary> options = default)
    {
        AVFormatContext* ctx = avformat_alloc_context();
        if (ctx == null) {
            throw new OutOfMemoryException("Could not allocate demuxer.");
        }
        
        ctx->pb = pb;
        AVDictionary* dict = options;
        
        avformat_open_input(&ctx, url.RawHandle, null, &dict).CheckError("Could not open input");
        avformat_find_stream_info(ctx, null).CheckError("Could not find stream information");
        av_dict_free(&dict);
        return ctx;
    }

    /// <summary> Find the "best" stream in the file. The best stream is determined according to various heuristics as the most likely to be what the user expects. </summary>
    public bool TryFindBestStream(AVMediaType type, out MediaStream stream)
    {
        unsafe
        {
            ThrowIfDisposed();
            var index = av_find_best_stream(_handle, type, -1, -1, null, 0);
        
            if (index < 0) {
                stream = default;
                return false;
            }
        
            stream = Streams[index];
            return true;
        }
    }

    /// <summary> Creates a decoder for the given audio or video stream. </summary>
    /// <param name="stream">Stream for which is supposed to be created the decoder</param>
    /// <param name="open">
    /// True to call <see cref="CodecBase.Open" /> before returning the decoder.
    /// Should be set to false if extra setup (e.g. hardware acceleration) is needed before opening.
    /// </param>
    public MediaDecoder CreateStreamDecoder(FFHandle<AVStream> stream, bool open = true)
    {
        unsafe
        {
            ThrowIfDisposed();

            if (Streams[stream.Ref.index].Handle != stream) {
                throw new ArgumentException("Specified stream is not owned by the demuxer.");
            }
        
            var codecPar = stream.Ref.codecpar;
            
            MediaDecoder decoder = stream.Ref.codecpar->codec_type switch {
                AVMediaType.AVMEDIA_TYPE_AUDIO => new AudioDecoder(codecPar->codec_id),
                AVMediaType.AVMEDIA_TYPE_VIDEO => new VideoDecoder(codecPar->codec_id),
                _ => throw new NotSupportedException($"Stream type {stream.Ref.codecpar->codec_type} is not supported."),
            };
            
            avcodec_parameters_to_context(decoder.Handle, codecPar).CheckError("Could not copy stream parameters to the decoder.");
        
            // Fixup some unset properties for consistency 
            decoder.TimeBase = stream.Ref.time_base;

            if (stream.Ref.codecpar->codec_type is AVMediaType.AVMEDIA_TYPE_VIDEO && decoder.FrameRate == Rational.Zero) {
                decoder.FrameRate = GuessFrameRate(stream);
            }

            if (open) decoder.Open();

            return decoder;
        }
    }

    /// <inheritdoc cref="ffmpeg.av_read_frame(AVFormatContext*, AVPacket*)"/>
    public bool Read(FFHandle<AVPacket> handle)
    {
        unsafe
        {
            ThrowIfDisposed();
            
            int result = av_read_frame(_handle, handle);

            if (result < 0 && result is not (int)AVError.AVERROR_EOF) {
                result.ThrowError(msg: "Failed to read packet");
            }
            return result >= 0;
        }
    }

    /// <summary> Seeks the demuxer to somewhere near <paramref name="timestamp"/>, according to <paramref name="options"/>. </summary>
    /// <param name="timestamp"></param>
    /// <param name="options"></param>
    /// <param name="stream">The stream to seek in. If null, a default stream is selected.</param>
    /// <remarks> If this method returns true, all open stream decoders must be flushed by calling <see cref="CodecBase.Flush"/>. </remarks>
    /// <exception cref="InvalidOperationException">If the underlying IO context doesn't support seeks.</exception>
    /// <exception cref="ArgumentException">If <paramref name="stream"/> is not owned by the demuxer.</exception>
    /// <returns>true if succeeded</returns>
    public bool Seek(TimeSpan timestamp, AVSEEK_FLAGS options = 0, MediaStream? stream = null)
    {
        ThrowIfDisposed();

        if (!CanSeek) {
            return false;
        }

        int streamIndex;
        long ts;
        
        if (stream is not null) {
            
            streamIndex = stream.Value.Index;
            if (Streams[streamIndex].Handle != stream.Value.Handle) {
                throw new ArgumentException("Specified stream is not owned by the demuxer.");
            }
            
            ts = av_rescale_q(timestamp.Ticks,
                new Rational(1, (int)TimeSpan.TicksPerSecond), stream.Value.TimeBase);
            
        } else {
            streamIndex = -1;
            ts = av_rescale(timestamp.Ticks, AV_TIME_BASE, TimeSpan.TicksPerSecond);
        }

        unsafe {
            return av_seek_frame(_handle, streamIndex, ts, (int)options) >= 0;
        }
    }

    /// <inheritdoc cref="ffmpeg.av_guess_frame_rate(AVFormatContext*, AVStream*, AVFrame*)"/>
    public Rational GuessFrameRate(FFHandle<AVStream> stream)
    {
        unsafe
        {
            ThrowIfDisposed();

            if (Streams[stream.Ref.index].Handle != stream) {
                throw new ArgumentException("Specified stream is not owned by the demuxer.");
            }
    
            return av_guess_frame_rate(_handle, stream, null);
        }
    }

    /// <inheritdoc />
    protected override unsafe void Free()
    {
        _ioContext?.Dispose();
        fixed (AVFormatContext** ptr = &_handle)
            avformat_close_input(ptr);
    }
}
