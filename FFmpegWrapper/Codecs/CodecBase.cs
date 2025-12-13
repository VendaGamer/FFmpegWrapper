namespace FFmpegWrapper.Codecs;

using System.Buffers;
using System.Runtime.InteropServices;

using Abstractions;
using Hardware;

public abstract class CodecBase : FFObject<AVCodecContext>
{
    public bool IsOpen {
        get {
            unsafe
            {
                return avcodec_is_open(Handle) != 0;
            }
        }
    }

    /// <inheritdoc cref="AVCodecContext.time_base"/>
    public Rational TimeBase {
        get => Handle.Ref.time_base;
        set => Handle.Ref.time_base = value;
    }

    /// <inheritdoc cref="AVCodecContext.framerate"/>
    public Rational FrameRate {
        get => Handle.Ref.framerate;
        set => Handle.Ref.framerate = value;
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
        get {
            unsafe
            {
                ThrowIfDisposed();
                return (_handle->codec->capabilities & (int)AVCodecCapabilities.AV_CODEC_CAP_DELAY) is not 0;
            }
        }
    }

    public AVMediaType CodecType {
        get {
            unsafe
            {
                return _handle->codec_type;
            }
        }
    }

    /// <inheritdoc cref="AVCodecContext.coded_side_data"/>
    public PacketSideDataList CodedSideData {
        get {
            unsafe {
                var raw = Handle.Raw;
                
                return new PacketSideDataList(&_handle->coded_side_data, &raw->nb_coded_side_data);
            }
        }
    }
    
    private IMemoryOwner<byte>? _extraData;

    protected CodecBase(FFHandle<AVCodecContext> ctx)
    {
        unsafe
        {
            _handle = ctx;
        }
    }

    protected static FFHandle<AVCodecContext> AllocContext(MediaCodec? codec)
    {
        unsafe
        {
            return avcodec_alloc_context3(codec is not null ? codec.Value.Raw : null);
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

    protected void SetHardwareContext(CodecHardwareConfig config, HardwareDevice device, HardwareFramePool? framePool)
    {
        unsafe
        {
            if (config.Codec.Raw != _handle->codec || config.DeviceType != device.Type) {
                throw new ArgumentException("Mismatching hardware codec config.");
            }
        
            _handle->hw_device_ctx = av_buffer_ref(device.Handle);
            _handle->hw_frames_ctx = framePool == null ? null : av_buffer_ref(framePool.Handle);

            if (framePool == null && (config.Methods & ~CodecHardwareMethods.FramesContext) == 0) {
                throw new ArgumentException("Specified hardware codec config requires a frame pool to be provided.");
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
            _extraData?.Dispose();
            var span = owner.Memory.Span;
            ref var handle = ref Handle.Ref; 
            
            if (span.IsEmpty) {
                handle.extradata = null;
                handle.extradata_size = 0;
                return;
            }

            _extraData = owner;
            
            handle.extradata = (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(span));
            handle.extradata_size = span.Length;
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void ThrowIfOpen()
    {
        if (IsOpen)
            throw new InvalidOperationException("Value must be set before the codec is open.");
    }

    /// <inheritdoc />
    protected override void Free()
    {
        unsafe
        {
            _extraData?.Dispose();
            
            fixed (AVCodecContext** c = &_handle) {
                avcodec_free_context(c);
            }
        }
    }
}
