namespace FFmpegWrapper.Codecs;

using Hardware;

public unsafe abstract class CodecBase : FFObject<AVCodecContext>
{
    protected bool _ownsContext = false;
    private bool _hasUserExtraData = false;
    public bool IsOpen => ffmpeg.avcodec_is_open(Handle) != 0;

    public readonly MediaCodec Codec;

    /// <inheritdoc cref="AVCodecContext.time_base"/>
    public Rational TimeBase {
        get => handle->time_base;
        set => SetOrThrowIfOpen(ref handle->time_base, value);
    }
    /// <inheritdoc cref="AVCodecContext.framerate"/>
    public Rational FrameRate {
        get => handle->framerate;
        set => SetOrThrowIfOpen(ref handle->framerate, value);
    }

    /// <summary>
    /// Some codecs need / can use extradata like Huffman tables. <br/>
    /// MJPEG: Huffman tables <br/>
    /// rv10: additional flags <br/>
    /// MPEG-4: global headers (they can be in the bitstream or here) <para/>
    /// 
    /// - encoding: Set by libavcodec.<br/>
    /// - decoding: Set by wrapper/user.
    /// </summary>
    public ReadOnlySpan<byte> ExtraData {
        get => new(handle->extradata, handle->extradata_size);
        set => SetExtraData(value);
    }

    /// <summary> Indicates if the codec requires flushing with NULL input at the end in order to give the complete and correct output. </summary>
    public bool IsDelayed => (handle->codec->capabilities & ffmpeg.AV_CODEC_CAP_DELAY) != 0;

    public AVMediaType CodecType => handle->codec_type;

    /// <inheritdoc cref="AVCodecContext.coded_side_data"/>
    public PacketSideDataList CodedSideData => new(&handle->coded_side_data, &handle->nb_coded_side_data); 

    internal CodecBase(AVCodecContext* ctx, AVMediaType expectedType, bool takeOwnership)
    {
        if (ctx->codec->type != expectedType) {
            if (takeOwnership) ffmpeg.avcodec_free_context(&ctx);
            
            throw new ArgumentException("Specified codec is not valid for the current media type.");
        }
        handle = ctx;
        _ownsContext = takeOwnership;
    }

    protected static AVCodecContext* AllocContext(MediaCodec codec)
    {
        var ctx = ffmpeg.avcodec_alloc_context3(codec.handle);

        if (ctx == null) {
            throw new OutOfMemoryException("Failed to allocate codec context.");
        }
        return ctx;
    }

    /// <summary> Initializes the codec if not already. </summary>
    public void Open()
    {
        if (!IsOpen) {
            ffmpeg.avcodec_open2(Handle, null, null).CheckError("Could not open codec");
        }
    }

    /// <summary> Enables or disables multi-threading if supported by the codec implementation. </summary>
    /// <param name="threadCount">Number of threads to use. 1 to disable multi-threading, 0 to automatically pick a value.</param>
    /// <param name="preferFrameSlices">Allow only multi-threaded processing of frame slices rather than individual frames. Setting to true may reduce delay. </param>
    public void SetThreadCount(int threadCount, bool preferFrameSlices = false)
    {
        ThrowIfOpen();
        
        int caps = handle->codec->capabilities;

        if ((caps & ffmpeg.AV_CODEC_CAP_SLICE_THREADS) != 0 && preferFrameSlices) {
            handle->thread_type = ffmpeg.FF_THREAD_SLICE;
            handle->thread_count = threadCount;
        }
        else if ((caps & ffmpeg.AV_CODEC_CAP_FRAME_THREADS) != 0) {
            handle->thread_type = ffmpeg.FF_THREAD_FRAME;
            handle->thread_count = threadCount;
        } else {
            handle->thread_type = 0;
            handle->thread_count = 1; //no multi-threading capability
        }
    }

    protected void SetHardwareContext(CodecHardwareConfig config, HardwareDevice device, HardwareFramePool? framePool)
    {
        if (config.Codec.handle != handle->codec || config.DeviceType != device.Type) {
            throw new ArgumentException("Mismatching hardware codec config.");
        }
        
        handle->hw_device_ctx = ffmpeg.av_buffer_ref(device.Handle);
        handle->hw_frames_ctx = framePool == null ? null : ffmpeg.av_buffer_ref(framePool.Handle);

        if (framePool == null && (config.Methods & ~CodecHardwareMethods.FramesContext) == 0) {
            throw new ArgumentException("Specified hardware codec config requires a frame pool to be provided.");
        }
    }

    /// <summary> Reset the decoder state / flush internal buffers. </summary>
    public virtual void Flush()
    {
        if (!IsOpen) {
            throw new InvalidOperationException("Cannot flush closed codec");
        }
        ffmpeg.avcodec_flush_buffers(Handle);
    }

    private bool SetExtraData(ReadOnlySpan<byte> buf)
    {
        ThrowIfOpen();
        
        if (buf.IsEmpty) {
            handle->extradata = null;
            handle->extradata_size = 0;
            return true;
        }
        
        if (handle->extradata != null)
            ffmpeg.av_freep(&handle->extradata);
        
        var data = (byte*)ffmpeg.av_mallocz((ulong)buf.Length + ffmpeg.AV_INPUT_BUFFER_PADDING_SIZE);
        if (data == null) {
            return false;
        }
        
        handle->extradata = data;
        handle->extradata_size = buf.Length;
        buf.CopyTo(new Span<byte>(handle->extradata, buf.Length));
        _hasUserExtraData = true;
        
        return true;
    }

    protected void SetOrThrowIfOpen<T>(ref T loc, T value)
    {
        ThrowIfOpen();
        loc = value;
    }

    protected void ThrowIfOpen()
    {
        if (IsOpen)
            throw new InvalidOperationException("Value must be set before the codec is open.");
    }

    /// <inheritdoc />
    protected override void Free()
    {
        if (handle != null) {
            if (_hasUserExtraData) {
                ffmpeg.av_freep(&handle->extradata);
            }
            if (_ownsContext) {
                fixed (AVCodecContext** c = &handle) {
                    ffmpeg.avcodec_free_context(c);
                }
            } else {
                handle = null;
            }
        }
    }
}