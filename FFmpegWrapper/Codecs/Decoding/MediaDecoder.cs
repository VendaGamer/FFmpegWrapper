namespace FFmpegWrapper.Codecs.Decoding;

using System.Buffers;
using Extensions;

public abstract class MediaDecoder : CodecBase
{
    public IMemoryOwner<byte>? ExtraData {
        protected get;
        set {
            unsafe {
                var handle = Handle.Raw;
                field?.Dispose();
                
                if (value is null) {
                    handle->extradata = null;
                    handle->extradata_size = 0;
                    return;
                }

                field = value;
                var span = value.Memory.Span;
                handle->extradata = span.RawHandle;
                handle->extradata_size = span.Length;
            }
        }
    }
    
    #region Constructors

    protected MediaDecoder(Handle<AVCodecContext> ctx) : base(ctx)
    {
        unsafe {
            if (av_codec_is_decoder(ctx.Raw->codec) is 0)
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
    

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected override void FreeManaged()
    {
        ExtraData?.Dispose();
    }
}
