namespace FFmpegWrapper.Media.Muxing;

using FFmpegWrapper.Codecs.Encoding;
using FFmpegWrapper.Extensions;
using FFmpegWrapper.Media.Streams;

public sealed class MediaMuxer : FFObject<AVFormatContext>
{

    #region Properties

    public ReadOnlySpan<MediaStream> Streams {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            unsafe
            {
                ref var handle = ref Handle.Ref;
            
                return new ReadOnlySpan<MediaStream>(handle.streams, (int)handle.nb_streams);
            }
        }
    }
    
    internal MediaPacket TempPacket => _tempPacket ??= new MediaPacket();
    
    public bool IsOpen { get; private set; }

    public OutputFormat OutputFormat {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            unsafe
            {
                return new OutputFormat(Handle.Ref.oformat);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set {
            unsafe {
                Handle.Ref.oformat = value.Handle;
            }
        }
    }

    #endregion

    #region Fields

    private readonly IHandleOwner<AVIOContext>? _ownedIOContext;
    
    private MediaPacket? _tempPacket;

    #endregion
    
    #region Constructors

    public unsafe MediaMuxer() : this(avformat_alloc_context())
    {
        
    }
    
    public MediaMuxer(ReadOnlySpan<byte> filename)
    {
        unsafe
        {
            fixed (AVFormatContext** fmtCtx = &_handle) {
                avformat_alloc_output_context2(fmtCtx, null, null, filename.RawHandle)
                    .CheckError("Could not allocate muxer");
            }
            
            avio_open(&_handle->pb, filename.RawHandle, (int)AVIO_FLAGS.AVIO_FLAG_WRITE).CheckError("Could not open output file");
        }

    }

    public MediaMuxer(IHandleOwner<AVIOContext> ioContext, ReadOnlySpan<byte> formatExtension)
        : this(ioContext, OutputFormat.FindByExtension(formatExtension).Handle)
    {
        _ownedIOContext = ioContext;
    }

    public unsafe MediaMuxer(
        IHandleOwner<AVIOContext> ioContext,
        Handle<AVOutputFormat> outputFormat) : this()
    {
        _handle->oformat = outputFormat;
        _handle->pb = ioContext.Handle;
    }
    
    public MediaMuxer(Handle<AVFormatContext> handle) : base(handle) { }

    public MediaMuxer(Handle<AVIOContext> ioContextHandle)
    {
        unsafe {
            _handle = avformat_alloc_context();
            _handle->pb = ioContextHandle;
        }
    }

    public MediaMuxer(
        Handle<AVIOContext> ioContextHandle,
        Handle<AVFormatContext> formatContext)
    {
        unsafe {
            _handle = formatContext;
        }
    }
    
    #endregion

    #region Methods

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
            
            var encHandle = encoder.Handle.Raw;
            
            AVStream* stream = avformat_new_stream(_handle, encHandle->codec);
            if (stream is null)
                throw new OutOfMemoryException("Could not allocate stream");
                
            stream->id = (int)_handle->nb_streams - 1;
            stream->time_base = encoder.TimeBase;

            avcodec_parameters_from_context(stream->codecpar, encHandle);
            
            if ((_handle->oformat->flags & (int)AVFormatCapabilityFlags.AVFMT_GLOBALHEADER) != 0) {
                encHandle->flags |= (int)AV_CODEC_FLAGS.AV_CODEC_FLAG_GLOBAL_HEADER;
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

            AVStream* stream = avformat_new_stream(_handle, null);
            if (stream == null) {
                throw new OutOfMemoryException("Could not allocate stream");
            }

            avcodec_parameters_copy(stream->codecpar,
                srcStream.Handle.Ref.codecpar).CheckError("Failed to copy codec parameters");
            
            stream->codecpar->codec_tag = 0;

            stream->id = (int)_handle->nb_streams - 1;
            stream->time_base = srcStream.TimeBase;

            var st = new MediaStream(stream);
            return st;
        }
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="options"></param>
    /// <param name="ignoreUnknownOptions"></param>
    /// <exception cref="InvalidOperationException"></exception>
    public void Open(ReadOnlySpan<Utf8KeyValue> options = default, bool ignoreUnknownOptions = false)
    {
        unsafe
        {
            ThrowIfDisposed();
            if (IsOpen) {
                throw new InvalidOperationException("Muxer is already open.");
            }
            
            AVDictionary* opts = null;
        
            foreach (var entry in options) {
                av_dict_set(&opts, entry.Key.RawHandle, entry.Value.RawHandle, 0).CheckError();
            }

            avformat_write_header(_handle, &opts).CheckError("Could not write header to output file");

            try {
                if (!ignoreUnknownOptions && av_dict_count(opts) > 0) {
                    string invalidKeys = string.Join("', '", new MediaDictionaryOwner(opts));
                    throw new InvalidOperationException($"Unknown or invalid muxer options (keys: '{invalidKeys}')");
                }
            }
            finally {
                av_dict_free(&opts);
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
    public void Write(Handle<AVPacket> packet)
    {
        unsafe
        {
            ThrowIfNotOpen();

            av_interleaved_write_frame(_handle, packet).CheckError("Failed to write packet");
        }
    }

    /// <summary> Encodes the given frame and muxes the resulting packets to the output file. </summary>
    public void EncodeAndWrite(MediaStream stream, MediaEncoder encoder, Handle<AVFrame> frame)
    {
        unsafe
        {
            avformat_write_header(_handle, null).CheckError("Could not write header to output file");
            _tempPacket ??= new MediaPacket();
            encoder.SendFrame(frame);

            while (encoder.ReceivePacket(_tempPacket)) {

                _tempPacket.RescaleTS(encoder.TimeBase, stream.TimeBase);
                _tempPacket.StreamIndex = stream.Index;
                av_interleaved_write_frame(_handle, _tempPacket.Handle).CheckError("Failed to write packet");
            }
            
            av_write_trailer(_handle);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ThrowIfNotOpen()
    {
        ThrowIfDisposed();

        if (!IsOpen) {
            throw new InvalidOperationException("Muxer is not open");
        }
    }

    public unsafe void WriteHeader(NullableHandle<AVDictionary> options = default)
    {
        AVDictionary* opt = options;
        avformat_write_header(Handle.Raw, &opt);
        IsOpen = true;
    }
    

    public unsafe void WriteTrailer()
    {
        av_write_trailer(Handle.Raw);
    }

    protected override void FreeManaged()
    {
        _ownedIOContext?.Dispose();
        _tempPacket?.Dispose();
    }

    /// <inheritdoc />
    protected unsafe override void Free()
    {
        if (IsOpen) {
            av_write_trailer(_handle);
        }
        avformat_free_context(_handle);
    }

    #endregion
    
}
