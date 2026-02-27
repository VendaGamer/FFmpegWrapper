namespace FFmpegWrapper.Codecs;

using System.Buffers;
using System.Runtime.InteropServices;
using Hardware;

public abstract class CodecBase : FFObject<AVCodecContext>
{
    
    public bool IsOpen {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            unsafe
            {
                return avcodec_is_open(Handle) is not 0;
            }
        }
    }

    /// <inheritdoc cref="AVCodecContext.time_base"/>
    public ref Rational TimeBase {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            unsafe {
                return ref Unsafe.AsRef<Rational>(&Handle.Raw->time_base);
            }
        }
    }

    /// <inheritdoc cref="AVCodecContext.framerate"/>
    public ref Rational FrameRate {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            unsafe {
                return ref Unsafe.AsRef<Rational>(&Handle.Raw->framerate);
            }
        }
    }

    /// <summary>
    /// Some codecs need / can use extradata like Huffman tables. <br/>
    /// MJPEG: Huffman tables <br/>
    /// rv10: additional flags <br/>
    /// MPEG-4: global headers (they can be in the bitstream or here) <para/>
    /// - encoding: Set by libavcodec.<br/>
    /// - decoding: Set by wrapper/user.
    /// </summary>
    
    public ReadOnlySpan<byte> ExtraData {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            unsafe
            {
                ThrowIfDisposed();

                if (_handle->extradata is null) {
                    return ReadOnlySpan<byte>.Empty;
                }

                return new ReadOnlySpan<byte>(_handle->extradata, _handle->extradata_size);
            }
        }
    }
    
    /// <summary> Indicates if the codec requires flushing with NULL input at the end in order to give the complete and correct output. </summary>
    public bool IsDelayed {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            unsafe
            {
                ThrowIfDisposed();
                return (_handle->codec->capabilities & (int)AVCodecCapabilities.AV_CODEC_CAP_DELAY) is not 0;
            }
        }
    }
    
    public AVMediaType CodecType {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Handle.Ref.codec_type;
    }

    /// <inheritdoc cref="AVCodecContext.coded_side_data"/>
    public PacketSideDataList CodedSideData {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            unsafe {
                var raw = Handle.Raw;
                
                return new PacketSideDataList(&_handle->coded_side_data, &raw->nb_coded_side_data);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected unsafe CodecBase(NullableHandle<AVCodec> codec = default)
        : this(avcodec_alloc_context3(codec))
    {
        Testk(ref _handle);
    }
    
    private unsafe void Testk(ref AVCodecContext* handle)
    {
        var idk = new ReadOnlySpan<AVCodecContext>(handle, 0);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected CodecBase(Handle<AVCodecContext> handle) : base(handle)
    {
        unsafe {
            ref var ctx = ref handle.Ref;
            
            ctx.execute =
                (delegate* unmanaged[Cdecl]<AVCodecContext*, delegate* unmanaged[Cdecl]<AVCodecContext*, void*, int>, void*, int*, int, int, int>)
                Marshal.GetFunctionPointerForDelegate(Execute);
            
            ctx.execute2 = 
                (delegate* unmanaged[Cdecl]<AVCodecContext*, delegate* unmanaged[Cdecl]<AVCodecContext*, void*, int, int, int>, void*, int*, int, int>)
                Marshal.GetFunctionPointerForDelegate(Execute2);
        }
    }

    public Handle<AVCodec> Codec {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            unsafe
            {
                return Handle.Ref.codec;
            }
        }
    }

    /// <summary> Initializes the codec if not already. </summary>
    /// <returns>false if already open</returns>
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Open()
    {
        if (IsOpen)
            return false;
        
        unsafe
        {
            avcodec_open2(Handle, null, null).CheckError("Could not open codec");
        }

        return true;
    }

    /// <summary> Enables or disables multi-threading if supported by the codec implementation. </summary>
    /// <param name="threadCount">Number of threads to use. 1 to disable multi-threading, 0 to automatically pick a value.</param>
    /// <param name="preferFrameSlices">Allow only multithreaded processing of frame slices rather than individual frames. Setting to true may reduce delay. </param>
    public void SetThreadCount(int threadCount, bool preferFrameSlices = false)
    {
        unsafe
        {
            ThrowIfOpen();
            ThrowIfDisposed();
            
            int caps = _handle->codec->capabilities;
            
            if ((caps & (int)AVCodecCapabilities.AV_CODEC_CAP_SLICE_THREADS) != 0 && preferFrameSlices) {
                _handle->thread_type = FF_THREAD_SLICE;
                _handle->thread_count = threadCount;
            }
            else if ((caps & (int)AVCodecCapabilities.AV_CODEC_CAP_FRAME_THREADS) != 0) {
                _handle->thread_type = FF_THREAD_FRAME;
                _handle->thread_count = threadCount;
            } else {
                _handle->thread_type = 0;
                _handle->thread_count = 1; //no multi-threading capability
            }
        }
    }

    /// <summary> Reset the decoder state / flush internal buffers. </summary>
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual void Flush()
    {
        unsafe
        {
            if (!IsOpen) {
                throw new InvalidOperationException("Cannot flush closed codec");
            }
            avcodec_flush_buffers(Handle);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ClearExtraData()
    {
        unsafe
        {
            ThrowIfOpen();
            ThrowIfDisposed();
            
            _handle->extradata = null;
            _handle->extradata_size = 0;
        }
    }

    private void SetExtraData(IMemoryOwner<byte> owner)
    {
        unsafe
        {
            ThrowIfOpen();
            var span = owner.Memory.Span;
            ref var handle = ref Handle.Ref; 
            
            if (span.IsEmpty) {
                handle.extradata = null;
                handle.extradata_size = 0;
                return;
            }
            
            handle.extradata = (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(span));
            handle.extradata_size = span.Length;
        }
    }
    
    protected void SetHardwareContext(
        CodecHardwareConfig config,
        HardwareDevice device,
        NullableHandle<AVBufferRef> framePool)
    {
        unsafe
        {
            if (config.Codec._handle != _handle->codec || config.DeviceType != device.Type) {
                throw new ArgumentException("Mismatching hardware codec config.");
            }
        
            _handle->hw_device_ctx = av_buffer_ref(device.Buffer.Handle);
            _handle->hw_frames_ctx = framePool.IsNull ? null : av_buffer_ref(framePool.Handle);

            if (framePool == null && (config.Methods & ~AV_CODEC_HW_CONFIG_METHOD.AV_CODEC_HW_CONFIG_METHOD_HW_FRAMES_CTX) is 0) {
                throw new ArgumentException("Specified hardware codec config requires a frame pool to be provided.");
            }
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void ThrowIfOpen()
    {
        if (IsOpen)
            throw new InvalidOperationException("Value must be set before the codec is open.");
    }

    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected virtual unsafe int Execute(
        AVCodecContext* ctx,
        delegate* unmanaged[Cdecl]<AVCodecContext*, void*, int> function,
        void* arg, int* ret, int count, int size) 
            => avcodec_default_execute(ctx, function, arg, ret, count, size);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected virtual unsafe int Execute2(
        AVCodecContext* ctx,
        delegate* unmanaged[Cdecl]<AVCodecContext*, void*, int, int, int> function,
        void* arg, int* ret, int count)
            => avcodec_default_execute2(ctx, function, arg, ret, count);

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected sealed override void Free()
    {
        unsafe
        {
            fixed (AVCodecContext** c = &_handle) {
                avcodec_free_context(c);
            }
        }
    }
}
