namespace FFmpegWrapper.Codecs.Decoding;

using Extensions;

public abstract class MediaDecoder(FFHandle<AVCodecContext> ctx) : CodecBase(ctx)
{
    public void SendPacket(MediaPacket? packet)
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

    /// <inheritdoc cref="ffmpeg.avcodec_send_packet(AVCodecContext*, AVPacket*)"/>
    public LavResult TrySendPacket(MediaPacket? packet)
    {
        unsafe
        {
            return (LavResult)avcodec_send_packet(Handle, packet!.Handle);
        }
    }
    
    public bool ReceiveFrame(MediaFrame frame)
    {
        unsafe
        {
            ThrowIfDisposed();
            var result = avcodec_receive_frame(_handle, frame.Handle);
        
            // Fast path for success (most common case)
            if (result == 0) return true;
        
            // // Fast path for common non-error cases
            // if (result == AVERROR(EAGAIN) || result == AVERROR_EOF) {
            //     return false;
            // }
        
            // Only throw for actual errors
            ((LavResult)result).ThrowIfError("Could not decode frame");
            return false;
        }
    }
    
    /// <summary>
    /// Batch processing method for improved throughput
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int ProcessPackets(ReadOnlySpan<MediaPacket> packets, Span<MediaFrame> frames)
    {
        /*int framesDecoded = 0;
        
        foreach (var packet in packets) {
            unsafe
            {
                var sendResult = avcodec_send_packet(_handle, packet.Handle);
                if (sendResult != 0 && sendResult != AVERROR_EOF) {
                    ((LavResult)sendResult).ThrowIfError("Could not send packet");
                    continue;
                }

                // Try to receive multiple frames from this packet
                for (int i = framesDecoded; i < frames.Length; i++) {
                    var receiveResult = avcodec_receive_frame(_handle, frames[i].Handle);
                
                    if (receiveResult == 0) {
                        framesDecoded++;
                    } else if (receiveResult == AVERROR(EAGAIN)) {
                        break; // Need more input
                    } else if (receiveResult == AVERROR_EOF) {
                        return framesDecoded; // End of stream
                    } else {
                        ((LavResult)receiveResult).ThrowIfError("Could not receive frame");
                        break;
                    }
                }
            }
        }*/
        
        return 0;
    }
    
}
