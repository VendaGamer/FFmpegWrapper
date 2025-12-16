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
    public LavResult TrySendPacket(FFHandle<AVPacket> handle)
    {
        unsafe
        {
            return (LavResult)avcodec_send_packet(Handle, handle);
        }
    }
    
    public bool ReceiveFrame(FFHandle<AVFrame> handle)
    {
        unsafe
        {
            ThrowIfDisposed();
            var result = (LavResult)avcodec_receive_frame(_handle, handle);
            
            if (result is 0) 
                return true;
            if (result is LavResult.TryAgain or LavResult.EndOfFile)
                return false;
            
            (result).ThrowIfError("Could not decode frame");
            return false;
        }
    }
    
    /// <summary>
    /// Batch processing method for improved throughput
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int ProcessPackets(ReadOnlySpan<MediaPacket> packets, Span<MediaFrame> frames)
    {
        int framesDecoded = 0;
        
        foreach (var packet in packets) {
            unsafe
            {
                var sendResult = avcodec_send_packet(_handle, packet.Handle);
                if (sendResult is not 0 && sendResult is not -11) {
                    ((LavResult)sendResult).ThrowIfError("Could not send packet");
                    continue;
                }

                // Try to receive multiple frames from this packet
                for (int i = framesDecoded; i < frames.Length; i++) {
                    var receiveResult = avcodec_receive_frame(_handle, frames[i].Handle);
                
                    if (receiveResult == 0) {
                        framesDecoded++;
                    } else if (receiveResult is -11) {
                        break; // Need more input
                    } else if (receiveResult is (int)AVError.AVERROR_EOF) {
                        return framesDecoded; // End of stream
                    } else {
                        ((LavResult)receiveResult).ThrowIfError("Could not receive frame");
                        break;
                    }
                }
            }
        }
        
        return framesDecoded;
    }
    
}
