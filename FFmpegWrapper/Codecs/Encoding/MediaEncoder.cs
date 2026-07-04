namespace FFmpegWrapper.Codecs.Encoding;

using Extensions;

public abstract class MediaEncoder : CodecBase
{

    /// <inheritdoc cref="AVCodecContext.bit_rate" />
    public long BitRate {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Handle.Ref.bit_rate;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set {
            ThrowIfOpen();
            Handle.Ref.bit_rate = value;
        }
    }

    /// <inheritdoc cref="AVCodecContext.global_quality" />
    public int GlobalQuality {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Handle.Ref.global_quality;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set {
            ThrowIfOpen();
            Handle.Ref.global_quality = value;
        }
    }

    /// <inheritdoc cref="AVCodecContext.compression_level" />
    public int CompressionLevel {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Handle.Ref.compression_level;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set {
            ThrowIfOpen();
            Handle.Ref.compression_level = value;
        }
    }

    /// <summary> Sets a codec specific option. If it doesn't exist, throws <see cref="InvalidOperationException"/>. </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetOption(ReadOnlySpan<byte> name, ReadOnlySpan<byte> value)
    {
        unsafe
        {
            av_opt_set(Handle.Ref.priv_data, name.RawHandle, value.RawHandle, 0).CheckError();
        }
    }

    /// <summary> Sets the value for a generic codec option. Note that these values may be ignored or unbalanced for some codecs. </summary>
    /// <remarks> https://ffmpeg.org/ffmpeg-codecs.html#Codec-Options </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetGlobalOption(ReadOnlySpan<byte> name, ReadOnlySpan<byte> value)
    {
        unsafe
        {
            av_opt_set(Handle, name.RawHandle, value.RawHandle, 0).CheckError();
        }
    }

    #region Constructors

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected MediaEncoder(Handle<AVCodecContext> ctx) : base(ctx)
    {

    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected MediaEncoder(NullableHandle<AVCodec> codec = default) : base(codec)
    {
        unsafe {
            if (codec.IsNull)
                return;
            if (av_codec_is_encoder(codec) is 0)
                throw new ArgumentException("Codec is not a encoder");
        }
    }

    #endregion
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe AVError ReceivePacket(Handle<AVPacket> packetHandle) => (AVError)avcodec_receive_packet(Handle, packetHandle);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe AVError SendFrame(NullableHandle<AVFrame> frame) => (AVError)avcodec_send_frame(Handle.Raw, frame);

    /// <summary> Returns a presentation timestamp (PTS) in terms of <see cref="CodecBase.TimeBase"/> for the given timespan. </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long GetFramePts(TimeSpan time)
    {
        return GetFramePts(time.Ticks, new Rational(1, (int)TimeSpan.TicksPerSecond));
    }
    /// <summary> Rescales the given timestamp to be in terms of <see cref="CodecBase.TimeBase"/>. </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long GetFramePts(long pts, Rational timeBase)
    {
        return av_rescale_q(pts, timeBase, TimeBase);
    }
}
