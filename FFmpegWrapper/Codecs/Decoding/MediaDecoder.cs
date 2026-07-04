namespace FFmpegWrapper.Codecs.Decoding;

using System.Buffers;

using Extensions;

public abstract class MediaDecoder : CodecBase
{
    /// <summary>
    /// Will be freed after calling Free
    /// </summary>
    protected internal MemoryHandle _extraDataHandle;
    
    
    /// <summary>
    /// Pass defualt to clear extraData
    /// </summary>
    /// <param name="extraData"></param>
    public void SetExtraData(ReadOnlyMemory<byte> extraData)
    {
        unsafe {
            var handle = Handle.Raw;
            if (extraData.IsEmpty) {
                handle->extradata = null;
                handle->extradata_size = 0;
                _extraDataHandle.Dispose();
                _extraDataHandle = default;
                return;
            }
            
            _extraDataHandle = extraData.Pin();
            handle->extradata = (byte*)_extraDataHandle.Pointer;
            handle->extradata_size = extraData.Length;
        }
    }
    
    #region Constructors

    protected MediaDecoder(Handle<AVCodecContext> ctxHandle) : base(ctxHandle)
    {
        unsafe {
            if (av_codec_is_decoder(ctxHandle.Raw->codec) is 0)
                throw new ArgumentException("Codec is not a decoder");
        }
    }
    
    protected MediaDecoder(NullableHandle<AVCodec> codec = default) : base(codec)
    {
        unsafe {
            if (codec.IsNull)
                return;
            if (av_codec_is_decoder(codec) is 0)
                throw new ArgumentException("Codec is not a decoder");
        }
    }

    protected MediaDecoder(AVCodecID codecId)
        : this(MediaCodec.GetDecoder(codecId).Handle)
    {
        
    }

    #endregion
    
    public AVError SendPacket(NullableHandle<AVPacket> packet)
    {
        unsafe
        {
            ThrowIfDisposed(); 
            return (AVError)avcodec_send_packet(_handle, packet!.Handle);
        }
    }

    /// <inheritdoc cref="avcodec_send_packet(AVCodecContext*, AVPacket*)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe AVError TrySendPacket(NullableHandle<AVPacket> handle) => (AVError)avcodec_send_packet(Handle, handle);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public AVError ReceiveFrame(Handle<AVFrame> handle)
    {
        unsafe
        {
            ThrowIfDisposed();
            return (AVError)avcodec_receive_frame(_handle, handle);
        }
    }

    protected override void Free()
    {
        _extraDataHandle.Dispose();
        base.Free();
    }
}
