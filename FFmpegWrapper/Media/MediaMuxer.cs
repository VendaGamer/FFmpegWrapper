namespace FFmpegWrapper.Media;

using Abstractions;

using Codecs.Encoding;
using Streams;

public sealed class MediaMuxer : FFObject<AVFormatContext>
{
    public ReadOnlySpan<MediaStream> Streams {
        get {
            unsafe
            {
                ref var handle = ref Handle.Ref;
            
                return new ReadOnlySpan<MediaStream>(handle.streams, (int)handle.nb_streams);
            }
        }
    }

    /// <inheritdoc cref="AVFormatContext.metadata" />
    public MediaDictionary Metadata;

    public bool IsOpen { get; private set; } = false;
    internal MediaPacket TempPacket => _tempPacket ??= new MediaPacket();
    
    private readonly IFFHandleOwner<AVIOContext>? _ownedIOContext;
    
    private MediaPacket? _tempPacket;

    public MediaMuxer(string filename)
    {
        unsafe
        {
            fixed (AVFormatContext** fmtCtx = &_handle) {
                ffmpeg.avformat_alloc_output_context2(fmtCtx, null, null, filename).CheckError("Could not allocate muxer");
            }
            ffmpeg.avio_open(&_handle->pb, filename, ffmpeg.AVIO_FLAG_WRITE).CheckError("Could not open output file");
            Metadata = new MediaDictionary(_handle->metadata);
        }

    }

    public MediaMuxer(IFFHandleOwner<AVIOContext> ioContext, ReadOnlySpan<char> formatExtension)
        : this(ioContext, OutputFormat.FindByExtenion(formatExtension).Handle)
    {
        _ownedIOContext = ioContext;
    }

    public MediaMuxer(IFFHandleOwner<AVIOContext> ioContext,
        FFHandle<AVOutputFormat> format)
    {
        unsafe
        {
            _handle = ffmpeg.avformat_alloc_context();
        
            if (_handle == null) {
                throw new OutOfMemoryException("Could not allocate muxer");
            }
        
            _handle->oformat = format;
            _handle->pb = ioContext.Handle;
        }
    }

    public MediaMuxer(FFHandle<AVIOContext> ioContextHandle)
    {
        unsafe {
            _handle = ffmpeg.avformat_alloc_context();
            _handle->pb = ioContextHandle;
        }
    }

    public MediaMuxer(
        FFHandle<AVIOContext> ioContextHandle,
        FFHandle<AVFormatContext> formatContext)
    {
        unsafe {
            _handle = formatContext;
        }
    }

    /// <summary> Creates and adds a new stream to the muxed file. </summary>
    /// <remarks> The <paramref name="encoder"/> must not be open before this is called. </remarks>
    public MediaStream AddStream(MediaEncoder encoder)
    {
        unsafe
        {
            ThrowIfDisposed();
            if (IsOpen) {
                throw new InvalidOperationException("Cannot add new streams once the muxer is open.");
            }
            if (encoder.IsOpen) {
                //This is an unfortunate limitation, but the GlobalHeader flag must be set before the encoder is open.
                throw new InvalidOperationException("Cannot add stream with an already open encoder.");
            }

            AVStream* stream = ffmpeg.avformat_new_stream(_handle, encoder.Handle.Raw->codec);
            if (stream == null) {
                throw new OutOfMemoryException("Could not allocate stream");
            }
            stream->id = (int)_handle->nb_streams - 1;
            stream->time_base = encoder.TimeBase;

            //Some formats want stream headers to be separate.
            if ((_handle->oformat->flags & ffmpeg.AVFMT_GLOBALHEADER) != 0) {
                encoder.Handle.Raw->flags |= ffmpeg.AV_CODEC_FLAG_GLOBAL_HEADER;
            }

            var st = new MediaStream(stream);
            
            return st;
        }
    }

    /// <summary>
    /// Creates and adds a new stream to the muxed file, copying the codec parameters from the source stream.
    /// </summary>
    public MediaStream AddStream(MediaStream srcStream)
    {
        unsafe
        {
            ThrowIfDisposed();
            if (IsOpen) {
                throw new InvalidOperationException("Cannot add new streams once the muxer is open.");
            }

            AVStream* stream = ffmpeg.avformat_new_stream(_handle, null);
            if (stream == null) {
                throw new OutOfMemoryException("Could not allocate stream");
            }

            ffmpeg.avcodec_parameters_copy(stream->codecpar,
                srcStream.Handle.Ref.codecpar).CheckError("Failed to copy codec parameters");
            
            stream->codecpar->codec_tag = 0;

            stream->id = (int)_handle->nb_streams - 1;
            stream->time_base = srcStream.TimeBase;

            var st = new MediaStream(stream);
            
            
            return st;
        }
    }

    /// <summary> Opens all streams and writes the container header. </summary>
    /// <remarks> This method will also open all encoders passed to <see cref="AddStream(MediaEncoder)"/>. </remarks>
    public void Open()
    {
        Open(Enumerable.Empty<KeyValuePair<string, string>>(), true);
    }

    /// <inheritdoc cref="Open()" />
    /// <param name="options">A collection of AVFormatContext and muxer-private options. </param>
    /// <param name="ignoreUnknownOptions">When false, throws <see cref="InvalidOperationException" /> when <paramref name="options" /> contains unknown or invalid entries. </param>
    public void Open(IEnumerable<KeyValuePair<string, string>> options, bool ignoreUnknownOptions = false)
    {
        unsafe
        {
            ThrowIfDisposed();
            if (IsOpen) {
                throw new InvalidOperationException("Muxer is already open.");
            }
            foreach (MediaStream stream in Streams) {
                //TODO:
                // ffmpeg.avcodec_parameters_from_context(stream.Handle.Ref.codecpar, some_handle)
                //     .CheckError("Could not copy the encoder parameters to the stream.");
                
            }

            AVDictionary* rawOpts = null;
            MediaDictionary.Populate(&rawOpts, options);

            ffmpeg.avformat_write_header(_handle, &rawOpts).CheckError("Could not write header to output file");

            try {
                if (!ignoreUnknownOptions && ffmpeg.av_dict_count(rawOpts) > 0) {
                    string invalidKeys = string.Join("', '", new MediaDictionary(rawOpts).Select(e => e.Key));
                    throw new InvalidOperationException($"Unknown or invalid muxer options (keys: '{invalidKeys}')");
                }
            } finally {
                ffmpeg.av_dict_free(&rawOpts);
            }
            IsOpen = true;
        }
    }

    /// <summary> Muxes the given packet to the output file, ensuring correct interleaving. </summary>
    /// <remarks>
    /// This function will buffer the packets internally as needed to make sure the
    /// packets in the output file are properly interleaved, usually ordered by
    /// increasing dts.
    /// </remarks>
    /// <param name="packet">
    /// This parameter can be null (at any time, not just at the end), to flush the interleaving queues.
    /// <br/>
    /// The <see cref="MediaPacket.StreamIndex"/> field must be
    /// set to the index of the corresponding stream in <see cref="Streams"/>.
    /// <br/>
    /// The timestamps (PTS and DTS) must be set to correct values in the stream's timebase (unless the
    /// output format is flagged with the AVFMT_NOTIMESTAMPS flag, then they can be set to AV_NOPTS_VALUE).
    /// The dts for subsequent packets in one stream must be strictly increasing (unless the output format 
    /// is flagged with the AVFMT_TS_NONSTRICT, then they merely have to be nondecreasing).
    /// Duration should also be set if known.
    /// <br/>
    /// On return, the packet will have been reset.
    /// </param>
    public void Write(MediaPacket? packet)
    {
        unsafe
        {
            ThrowIfNotOpen();

            ffmpeg.av_interleaved_write_frame(_handle, packet?.Handle ?? null).CheckError("Failed to write packet");
        }
    }

    /// <summary> Encodes the given frame and muxes the resulting packets to the output file. </summary>
    public void EncodeAndWrite(MediaStream stream, MediaEncoder encoder, MediaFrame? frame)
    {
        ThrowIfNotOpen();

        if (Streams[stream.Index].Handle != stream.Handle) {
            throw new ArgumentException("Specified stream is not owned by the muxer.");
        }
        
        _tempPacket ??= new MediaPacket();

        encoder.SendFrame(frame?.Handle ?? null);
        
        while (encoder.ReceivePacket(_tempPacket)) {
            unsafe
            {
                _tempPacket.RescaleTS(encoder.TimeBase, stream.TimeBase);
                _tempPacket.StreamIndex = stream.Index;
                ffmpeg.av_interleaved_write_frame(_handle, _tempPacket.Handle).CheckError("Failed to write packet");
            }
        }
    }

    private void ThrowIfNotOpen()
    {
        ThrowIfDisposed();

        if (!IsOpen) {
            throw new InvalidOperationException("Muxer is not open");
        }
    }

    /// <inheritdoc />
    protected unsafe override void Free()
    {
        ffmpeg.av_write_trailer(_handle);

        ffmpeg.avformat_free_context(_handle);
        
        fixed (AVFormatContext** ptr = &_handle) {
            ffmpeg.avformat_close_input(ptr);
        }

        _ownedIOContext?.Dispose();
        
        _tempPacket?.Dispose();
    }
}