namespace FFmpegWrapper.Media;

using Codecs;
using Codecs.Decoding;

using Streams;

public class MediaDemuxer : FFObject<AVFormatContext>
{
    public IOContext? IOC { get; }
    readonly bool _iocLeaveOpen;
    private bool _ownsCtx;

    public TimeSpan? Duration {
        get {
            unsafe
            {
                return Helpers.GetTimeSpan(Handle->duration, new Rational(1, ffmpeg.AV_TIME_BASE));
            }
        }
    }

    /// <summary> An array of all streams in the file. </summary>
    public ImmutableArray<MediaStream> Streams { get; }

    /// <inheritdoc cref="AVFormatContext.metadata" />
    public MediaDictionary Metadata {
        get {
            unsafe
            {
                return new(&Handle->metadata);
            }
        }
    }

    public bool CanSeek {
        get {
            unsafe
            {
                return Handle->pb->seek.Pointer != IntPtr.Zero;
            }
        }
    }

    /// <summary> Opens an existing resource URL for demuxing. </summary>
    /// <remarks>
    /// Note that this constructor accepts URLs for other than files, as supported by FFmpeg. <br/>
    /// If that is not desirable, ensure that <paramref name="url"/> points to a valid file path prior to instantiation (via <see cref="File.Exists(string)"/>), 
    /// or use <see cref="MediaDemuxer(string, IEnumerable{KeyValuePair{string, string}})"/> with the
    /// <c>protocol_whitelist=file</c> option.
    /// </remarks>
    public unsafe MediaDemuxer(string url)
        : this(CreateContext(url, null, null), takeOwnership: true) { }

    /// <inheritdoc />
    public unsafe MediaDemuxer(IOContext ioc, bool leaveOpen = false)
        : this(CreateContext(null, ioc.Handle, null), takeOwnership: true)
    {
        IOC = ioc;
        _iocLeaveOpen = leaveOpen;
    }

    /// <summary> Opens an existing resource URL for demuxing. </summary>
    /// <remarks> See https://ffmpeg.org/ffmpeg-formats.html, https://ffmpeg.org/ffmpeg-protocols.html </remarks>
    /// <param name="url">URL to be opened for demuxing</param>
    /// <param name="options"> A dictionary filled with AVFormatContext and demuxer-private options. </param>
    public unsafe MediaDemuxer(string url, IEnumerable<KeyValuePair<string, string>> options)
        : this(CreateContext(url, null, options), takeOwnership: true) { }

    /// <summary> Wraps a pointer to an open <see cref="AVFormatContext"/>. </summary>
    /// <param name="ctx"></param>
    /// <param name="takeOwnership">True if <paramref name="ctx"/> should be freed when Dispose() is called.</param>
    internal unsafe MediaDemuxer(AVFormatContext* ctx, bool takeOwnership)
    {
        handle = ctx;
        _ownsCtx = takeOwnership;
        var streams = ImmutableArray.CreateBuilder<MediaStream>((int)Handle->nb_streams);
        for (int i = 0; i < handle->nb_streams; i++) {
            streams.Add(new MediaStream(handle->streams[i]));
        }
        Streams = streams.MoveToImmutable();
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe AVFormatContext* CreateContext(string? url, AVIOContext* pb, IEnumerable<KeyValuePair<string, string>>? options)
    {
        AVFormatContext* ctx = ffmpeg.avformat_alloc_context();
        if (ctx == null) {
            throw new OutOfMemoryException("Could not allocate demuxer.");
        }

        ctx->pb = pb;

        AVDictionary* rawOpts = null;
        MediaDictionary.Populate(&rawOpts, options);
        
        ffmpeg.avformat_open_input(&ctx, url, null, &rawOpts).CheckError("Could not open input");

        try {
            if (ffmpeg.av_dict_count(rawOpts) > 0) {
                string invalidKeys = string.Join("', '", new MediaDictionary(&rawOpts).Select(e => e.Key));
                throw new InvalidOperationException($"Unknown or invalid demuxer options (keys: '{invalidKeys}')");
            }
        } finally {
            ffmpeg.av_dict_free(&rawOpts);
        }

        ffmpeg.avformat_find_stream_info(ctx, null).CheckError("Could not find stream information");
        return ctx;
    }

    /// <summary> Find the "best" stream in the file. The best stream is determined according to various heuristics as the most likely to be what the user expects. </summary>
    public bool TryFindBestStream(AVMediaType type, out MediaStream stream)
    {
        unsafe
        {
            ThrowIfDisposed();
            var index = ffmpeg.av_find_best_stream(handle, type, -1, -1, null, 0);
        
            if (index < 0) {
                stream = null!;
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
    public MediaDecoder CreateStreamDecoder(MediaStream stream, bool open = true)
    {
        unsafe
        {
            ThrowIfDisposed();

            if (Streams[stream.Index] != stream) {
                throw new ArgumentException("Specified stream is not owned by the demuxer.");
            }
        
            var codecPar = stream.Handle->codecpar;
            var decoder = stream.Type switch {
                MediaTypes.Audio => new AudioDecoder(codecPar->codec_id) as MediaDecoder,
                MediaTypes.Video => new VideoDecoder(codecPar->codec_id),
                _ => throw new NotSupportedException($"Stream type {stream.Type} is not supported."),
            };
            ffmpeg.avcodec_parameters_to_context(decoder.Handle, codecPar).CheckError("Could not copy stream parameters to the decoder.");
        
            // Fixup some unset properties for consistency 
            decoder.TimeBase = stream.TimeBase;

            if (stream.Type == MediaTypes.Video && decoder.FrameRate == Rational.Zero) {
                decoder.FrameRate = GuessFrameRate(stream);
            }

            if (open) decoder.Open();

            return decoder;
        }
    }

    /// <inheritdoc cref="ffmpeg.av_read_frame(AVFormatContext*, AVPacket*)"/>
    public bool Read(MediaPacket packet)
    {
        unsafe
        {
            ThrowIfDisposed();

            int result = ffmpeg.av_read_frame(handle, packet.UnrefAndGetHandle().Handle);

            if (result < 0 && result != ffmpeg.AVERROR_EOF) {
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
    public bool Seek(TimeSpan timestamp, SeekOptions options, MediaStream? stream = null)
    {
        ThrowIfDisposed();

        if (!CanSeek) {
            return false;
        }

        int streamIndex;
        long ts;
        if (stream != null) {
            streamIndex = stream.Index;
            if (Streams[streamIndex] != stream) {
                throw new ArgumentException("Specified stream is not owned by the demuxer.");
            }
            ts = ffmpeg.av_rescale_q(timestamp.Ticks, new Rational(1, (int)TimeSpan.TicksPerSecond), stream.TimeBase);
        } else {
            streamIndex = -1;
            ts = ffmpeg.av_rescale(timestamp.Ticks, ffmpeg.AV_TIME_BASE, TimeSpan.TicksPerSecond);
        }

        unsafe {
            return ffmpeg.av_seek_frame(handle, streamIndex, ts, (int)options) >= 0;
        }
    }

    /// <inheritdoc cref="ffmpeg.av_guess_frame_rate(AVFormatContext*, AVStream*, AVFrame*)"/>
    public Rational GuessFrameRate(MediaStream stream)
    {
        unsafe
        {
            ThrowIfDisposed();

            if (Streams[stream.Index] != stream) {
                throw new ArgumentException("Specified stream is not owned by the demuxer.");
            }
    
            var guessedRate = ffmpeg.av_guess_frame_rate(handle, stream.Handle, null);
    
            // Return the guessed rate (caller needs to validate)
            return guessedRate;
        }
    }

    /// <inheritdoc />
    protected override unsafe void Free()
    {
        if (handle != null && _ownsCtx) {
            fixed (AVFormatContext** c = &handle) ffmpeg.avformat_close_input(c);
        }
        if (!_iocLeaveOpen) {
            IOC?.Dispose();
        }
    }
}