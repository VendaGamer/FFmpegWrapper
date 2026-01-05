namespace FFmpegWrapper.Codecs.Encoding;

using Extensions;

public abstract class MediaEncoder : CodecBase
{

    /// <inheritdoc cref="AVCodecContext.bit_rate" />
    public long BitRate {
        get => Handle.Ref.bit_rate;
        set {
            ThrowIfOpen();
            Handle.Ref.bit_rate = value;
        }
    }

    /// <inheritdoc cref="AVCodecContext.global_quality" />
    public int GlobalQuality {
        get => Handle.Ref.global_quality;
        set {
            ThrowIfOpen();
            Handle.Ref.global_quality = value;
        }
    }

    /// <inheritdoc cref="AVCodecContext.compression_level" />
    public int CompressionLevel {
        get => Handle.Ref.compression_level;
        set {
            ThrowIfOpen();
            Handle.Ref.compression_level = value;
        }
    }

    /// <summary> Sets a codec specific option. If it doesn't exist, throws <see cref="InvalidOperationException"/>. </summary>
    public void SetOption(ReadOnlySpan<byte> name, ReadOnlySpan<byte> value)
    {
        unsafe
        {
            av_opt_set(Handle.Ref.priv_data, name.RawHandle, value.RawHandle, 0).CheckError();
        }
    }

    /// <summary> Sets the value for a generic codec option. Note that these values may be ignored or unbalanced for some codecs. </summary>
    /// <remarks> https://ffmpeg.org/ffmpeg-codecs.html#Codec-Options </remarks>
    public void SetGlobalOption(ReadOnlySpan<byte> name, ReadOnlySpan<byte> value)
    {
        unsafe
        {
            av_opt_set(Handle, name.RawHandle, value.RawHandle, 0).CheckError();
        }
    }

    #region Constructors

    protected MediaEncoder(FFHandle<AVCodecContext> ctx) : base(ctx)
    {

    }

    protected MediaEncoder(NullableFFHandle<AVCodec> codec = default) : base(codec)
    {
        unsafe {
            if (codec.IsNull)
                return;
            if (av_codec_is_encoder(codec) is 0)
                throw new ArgumentException("Codec is not a encoder");
        }
    }

    #endregion
    

    public bool ReceivePacket(FFHandle<AVPacket> packetHandle)
    {
        unsafe
        {
            var result = (LavResult)avcodec_receive_packet(Handle, packetHandle);
            
            if (result is not (LavResult.Success or LavResult.TryAgain or LavResult.EndOfFile)) {
                result.ThrowIfError("Could not encode packet");
            }
            return result >= 0;
        }
    }
    
    public bool SendFrame(NullableFFHandle<AVFrame> frame)
    {
        unsafe
        {
            var result = (LavResult)avcodec_send_frame(Handle.Raw, frame);

            if (result != LavResult.Success && result != LavResult.EndOfFile) {
                result.ThrowIfError("Could not encode frame");
            }
            
            return result >= 0;
        }
    }

    /// <summary> Returns a presentation timestamp (PTS) in terms of <see cref="CodecBase.TimeBase"/> for the given timespan. </summary>
    public long GetFramePts(TimeSpan time)
    {
        return GetFramePts(time.Ticks, new Rational(1, (int)TimeSpan.TicksPerSecond));
    }
    /// <summary> Rescales the given timestamp to be in terms of <see cref="CodecBase.TimeBase"/>. </summary>
    public long GetFramePts(long pts, Rational timeBase)
    {
        return av_rescale_q(pts, timeBase, TimeBase);
    }
}
