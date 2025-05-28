namespace FFmpeg.Wrapper;

public abstract unsafe class MediaDecoder : CodecBase
{
    public MediaDecoder(AVCodecContext* ctx, AVMediaType expectedType, bool takeOwnership)
        : base(ctx, expectedType, takeOwnership) { }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SendPacket(MediaPacket? packet)
    {
        var result = ffmpeg.avcodec_send_packet(_handle, packet!.Handle);
        
        // Fast path for success
        if (result == 0) return;
        
        // Only convert to enum and check for specific cases when needed
        var lavResult = (LavResult)result;
        if (!(lavResult == LavResult.EndOfFile && packet == null)) {
            lavResult.ThrowIfError("Could not decode packet");
        }
    }

    /// <inheritdoc cref="ffmpeg.avcodec_send_packet(AVCodecContext*, AVPacket*)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public LavResult TrySendPacket(MediaPacket? packet)
    {
        return (LavResult)ffmpeg.avcodec_send_packet(_handle, packet!.Handle);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ReceiveFrame(MediaFrame frame)
    {
        var result = ffmpeg.avcodec_receive_frame(_handle, frame.Handle);
        
        // Fast path for success (most common case)
        if (result == 0) return true;
        
        // Fast path for common non-error cases
        if (result == ffmpeg.AVERROR(ffmpeg.EAGAIN) || result == ffmpeg.AVERROR_EOF) {
            return false;
        }
        
        // Only throw for actual errors
        ((LavResult)result).ThrowIfError("Could not decode frame");
        return false;
    }
    
    /// <summary>
    /// Batch processing method for improved throughput
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int ProcessPackets(ReadOnlySpan<MediaPacket> packets, Span<MediaFrame> frames)
    {
        int framesDecoded = 0;
        
        foreach (var packet in packets) {
            var sendResult = ffmpeg.avcodec_send_packet(_handle, packet.Handle);
            if (sendResult != 0 && sendResult != ffmpeg.AVERROR_EOF) {
                ((LavResult)sendResult).ThrowIfError("Could not send packet");
                continue;
            }

            // Try to receive multiple frames from this packet
            for (int i = framesDecoded; i < frames.Length; i++) {
                var receiveResult = ffmpeg.avcodec_receive_frame(_handle, frames[i].Handle);
                
                if (receiveResult == 0) {
                    framesDecoded++;
                } else if (receiveResult == ffmpeg.AVERROR(ffmpeg.EAGAIN)) {
                    break; // Need more input
                } else if (receiveResult == ffmpeg.AVERROR_EOF) {
                    return framesDecoded; // End of stream
                } else {
                    ((LavResult)receiveResult).ThrowIfError("Could not receive frame");
                    break;
                }
            }
        }
        
        return framesDecoded;
    }
    
}