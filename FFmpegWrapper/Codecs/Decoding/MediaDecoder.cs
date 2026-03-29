namespace FFmpegWrapper.Codecs.Decoding;

using System.Buffers;
using Extensions;

public abstract class MediaDecoder : CodecBase
{
    /// <summary>
    /// Will be freed after calling Free
    /// </summary>
    protected internal MemoryHandle ExtraDataHandle;
    
    
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
                ExtraDataHandle.Dispose();
                ExtraDataHandle = default;
                return;
            }
            
            ExtraDataHandle = extraData.Pin();
            handle->extradata = (byte*)ExtraDataHandle.Pointer;
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
    
    public void SendPacket(NullableHandle<AVPacket> packet)
    {
        unsafe
        {
            ThrowIfDisposed();
        
            var result = avcodec_send_packet(_handle, packet!.Handle);
            // Fast path for success
            if (result == 0) return;
        
            // Only convert to enum and check for specific cases when needed
            var lavResult = (LavResult)result;
            
            if (lavResult != LavResult.EndOfFile) {
                lavResult.ThrowIfError("Could not decode packet");
            }
        }
    }

    /// <inheritdoc cref="avcodec_send_packet(AVCodecContext*, AVPacket*)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public LavResult TrySendPacket(NullableHandle<AVPacket> handle)
    {
        unsafe
        {
            return (LavResult)avcodec_send_packet(Handle, handle);
        }
    }
    
    public bool ReceiveFrame(Handle<AVFrame> handle)
    {
        unsafe
        {
            ThrowIfDisposed();
            var result = (LavResult)avcodec_receive_frame(_handle, handle);
            
            if (result is LavResult.TryAgain or LavResult.EndOfFile)
                return false;
            
            result.ThrowIfError("Could not decode frame");
            return true;
        }
    }

    protected override void Free()
    {
        ExtraDataHandle.Dispose();
        base.Free();
    }
}
