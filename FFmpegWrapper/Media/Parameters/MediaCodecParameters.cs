namespace FFmpegWrapper.Media.Parameters;

public abstract class MediaCodecParameters : FFObject<AVCodecParameters>
{
    /// <inheritdoc cref="AVCodecParameters.codec_type" />
    public ref AVMediaType CodecType {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref Handle.Ref.codec_type;
    }

    /// <inheritdoc cref="AVCodecParameters.codec_id" />
    public ref AVCodecID CodecId {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref Handle.Ref.codec_id;
    }

    /// <inheritdoc cref="AVCodecParameters.codec_tag" />
    public ref uint CodecTag {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref Handle.Ref.codec_tag;
    }

    /// <inheritdoc cref="AVCodecParameters.extradata" />
    public ReadOnlySpan<byte> ExtraData {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            unsafe
            {
                ref var handle = ref Handle.Ref;
                
                return new ReadOnlySpan<byte>(handle.extradata, handle.extradata_size);
            }
        }
    }

    /// <inheritdoc cref="AVCodecParameters.bit_rate" />
    public ref long BitRate {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref Handle.Ref.bit_rate;
    }

    /// <inheritdoc cref="AVCodecParameters.bits_per_coded_sample" />
    public ref int BitsPerCodedSample {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref Handle.Ref.bits_per_coded_sample;
    }

    /// <inheritdoc cref="AVCodecParameters.bits_per_raw_sample" />
    public ref int BitsPerRawSample {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref Handle.Ref.bits_per_raw_sample;
    }

    /// <inheritdoc cref="AVCodecParameters.profile" />
    public ref int Profile {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref Handle.Ref.profile;
    }

    /// <summary> Shorthand for <c>ffmpeg.avcodec_profile_name(CodecId, Profile)</c>. </summary>
    public ReadOnlySpan<byte> ProfileName {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            unsafe
            {
                return FFHelper.Utf8SpanFromPtrNullTerm(avcodec_profile_name(CodecId, Profile));
            }
        }
    }
    
    
    /// <summary>
    /// Levels constrain decoder capability requirements (for example, maximum resolution, bitrate,
    /// decoded picture buffer size, or macroblocks-per-second) for codecs that define
    /// profiles and levels such as H.264/AVC, HEVC, or MPEG-4 Part 2.
    ///
    /// The stored value is the integer level identifier specified by the codec
    /// standard (e.g., H.264 Level 4.1 is represented as <c>41</c>). For codecs
    /// that do not define levels, or when the level is unknown, this value is
    /// typically <c>0</c>.
    /// </summary>
    public ref int Level {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref Handle.Ref.level;
    }

    /// <inheritdoc cref="AVCodecParameters.coded_side_data"/>
    public PacketSideDataList CodedSideData {
        get {
            unsafe {
                var raw = Handle.Raw;
                
                return new PacketSideDataList(
                    &raw->coded_side_data,
                    new Handle<int>(&raw->nb_coded_side_data)
                );
            }
        }
    }
    
    protected MediaCodecParameters(Handle<AVCodecParameters> handle) : base(handle)
    {
        
    }

    protected unsafe MediaCodecParameters() : base(avcodec_parameters_alloc())
    {
        
    }

    public bool Equals(MediaCodecParameters other)
    {
        unsafe
        {
            return Handle.Raw == other.Handle.Raw;
        }
    }
    
    protected override unsafe void Free()
    {
        fixed (AVCodecParameters** ptr = &_handle) {
            avcodec_parameters_free(ptr);
        }
    }
}
