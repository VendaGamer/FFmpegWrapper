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

    public ref Handle<AVIOContext> IOCtx {
        get {
            unsafe {
                return ref *(Handle<AVIOContext>*)&Handle.Raw->pb;
            }
        }
    }
    
    internal MediaPacket TempPacket => _tempPacket ??= new MediaPacket();

    public OutputFormat OutputFormat {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            unsafe
            {
                return new OutputFormat((Handle<AVOutputFormat>)Handle.Ref.oformat);
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
    
    
    public MediaMuxer(
        ReadOnlySpan<byte> filename,
        ReadOnlySpan<byte> formatName = default,
        NullableHandle<AVOutputFormat> outputFormat = default)
    {
        unsafe
        {
            fixed (byte* pName = filename)
            fixed (byte* pFmtName = filename)
            {
                AVFormatContext* ctx = null;
                avformat_alloc_output_context2(&ctx, outputFormat, pFmtName, pName)
                    .CheckError("Could not allocate muxer");

                _handle = ctx;
            }
        }
    }

    public MediaMuxer(IHandleOwner<AVIOContext> ioContext, ReadOnlySpan<byte> formatExtension)
        : this(ioContext, OutputFormat.FindByExtension(formatExtension).Handle)
    {
        _ownedIOContext = ioContext;
    }

    public unsafe MediaMuxer(
        IHandleOwner<AVIOContext> ioContext,
        Handle<AVOutputFormat> outputFormat)
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
            
            var encHandle = encoder.Handle.Raw;
            
            AVStream* stream = avformat_new_stream(_handle, encHandle->codec);
            if (stream is null)
                throw new Exception("Could not allocate stream");
                
            stream->id = (int)_handle->nb_streams - 1;
            stream->time_base = encoder.TimeBase;

            avcodec_parameters_from_context(stream->codecpar, encHandle);
            
            if ((_handle->oformat->flags & (int)AVFormatCapabilityFlags.AVFMT_GLOBALHEADER) != 0) {
                encHandle->flags |= (int)AVCodecFlags.AV_CODEC_FLAG_GLOBAL_HEADER;
            }
            
            return *(MediaStream*)&stream;
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
            
            AVStream* stream = avformat_new_stream(_handle, null);
            return *(MediaStream*)&stream;
        }
    }

    public unsafe void InitOutput<T>(T source) where T : IHandleSource<AVDictionary>
    {
        fixed (AVDictionary** dict = &source.GetPinnableReference()) {
            avformat_init_output(Handle, dict);
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
    public void WriteInterleaved(Handle<AVPacket> packet)
    {
        unsafe
        {
            av_interleaved_write_frame(_handle, packet).CheckError("Failed to write packet");
        }
    }

    public void Write(Handle<AVPacket> packet)
    {
        unsafe
        {
            av_write_frame(_handle, packet).CheckError("Failed to write packet");
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

            while (encoder.ReceivePacket(_tempPacket).IsSuccess) {

                _tempPacket.RescaleTS(encoder.TimeBase, stream.TimeBase);
                _tempPacket.StreamIndex = stream.Index;
                av_interleaved_write_frame(_handle, _tempPacket.Handle).CheckError("Failed to write packet");
            }
            
            av_write_trailer(_handle);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe void WriteHeader() => avformat_write_header(Handle.Raw, null);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe void WriteHeader<T>(T? options = default)
        where T : IHandleSource<AVDictionary>
    {
        if (options is null) {
            WriteHeader();
            return;
        }
        
        fixed (AVDictionary** ptr = &options.GetPinnableReference()) {
            avformat_write_header(Handle.Raw, ptr);
        }
    }
    

    public unsafe bool WriteTrailer()
    {
        return av_write_trailer(Handle.Raw) is 0;
    }

    protected override void FreeManaged()
    {
        _ownedIOContext?.Dispose();
        _tempPacket?.Dispose();
    }

    /// <inheritdoc />
    protected unsafe override void Free()
    {
        avformat_free_context(_handle);
    }

    #endregion
    
}
