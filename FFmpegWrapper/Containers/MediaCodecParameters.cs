namespace FFmpegWrapper.Containers;

using Abstractions;

using Media;
using Media.Streams;

public class MediaCodecParameters : FFObject<AVCodecParameters>
{
    public MediaCodecParameters(FFHandle<AVCodecParameters> handle)
    {
        unsafe {
            _handle = handle;
        }
    }

    public MediaCodecParameters()
    {
        unsafe {
            _handle = ffmpeg.avcodec_parameters_alloc();
        }
    }
    
    /// <inheritdoc cref="AVCodecParameters.codec_type" />
    public MediaType CodecType => (MediaType)Handle.Ref.codec_type;

    /// <inheritdoc cref="AVCodecParameters.codec_id" />
    public AVCodecID CodecId => Handle.Ref.codec_id;

    /// <inheritdoc cref="AVCodecParameters.codec_tag" />
    public uint CodecTag => Handle.Ref.codec_tag;

    /// <inheritdoc cref="AVCodecParameters.extradata" />
    public ReadOnlySpan<byte> ExtraData {
        get {
            unsafe
            {
                return new ReadOnlySpan<byte>(Handle.Ref.extradata, Handle.Ref.extradata_size);
            }
        }
    }

    /// <inheritdoc cref="AVCodecParameters.bit_rate" />
    public long BitRate => Handle.Ref.bit_rate;

    /// <inheritdoc cref="AVCodecParameters.bits_per_coded_sample" />
    public int BitsPerCodedSample => Handle.Ref.bits_per_coded_sample;

    /// <inheritdoc cref="AVCodecParameters.bits_per_raw_sample" />
    public int BitsPerRawSample => Handle.Ref.bits_per_raw_sample;

    /// <inheritdoc cref="AVCodecParameters.profile" />
    public int Profile => Handle.Ref.profile;

    /// <summary> Shorthand for <c>ffmpeg.avcodec_profile_name(CodecId, Profile)</c>. </summary>
    public string ProfileName => ffmpeg.avcodec_profile_name(CodecId, Profile);

    /// <inheritdoc cref="AVCodecParameters.level" />
    public int Level => Handle.Ref.level;

    //Video fields

    /// <inheritdoc cref="AVCodecParameters.width" />
    public int Width => Handle.Ref.width;

    /// <inheritdoc cref="AVCodecParameters.width" />
    public int Height => Handle.Ref.height;

    public AVPixelFormat PixelFormat => (AVPixelFormat)Handle.Ref.format;

    /// <inheritdoc cref="AVCodecParameters.sample_aspect_ratio" />
    public Rational PixelAspectRatio => Handle.Ref.sample_aspect_ratio;

    public PictureFormat PictureFormat => new(Width, Height, PixelFormat, PixelAspectRatio);

    /// <inheritdoc cref="AVCodecParameters.framerate"/>
    public Rational FrameRate => Handle.Ref.framerate;

    /// <inheritdoc cref="AVCodecParameters.field_order" />
    public AVFieldOrder FieldOrder => Handle.Ref.field_order;

    /// <inheritdoc cref="AVCodecParameters.color_range" />
    public AVColorRange ColorRange => Handle.Ref.color_range;

    /// <inheritdoc cref="AVCodecParameters.color_primaries" />
    public AVColorPrimaries ColorPrimaries => Handle.Ref.color_primaries;

    /// <inheritdoc cref="AVCodecParameters.color_trc" />
    public AVColorTransferCharacteristic ColorTrc => Handle.Ref.color_trc;

    /// <inheritdoc cref="AVCodecParameters.color_space" />
    public AVColorSpace ColorMatrix => Handle.Ref.color_space;

    /// <inheritdoc cref="AVCodecParameters.chroma_location" />
    public AVChromaLocation ChromaLocation => Handle.Ref.chroma_location;

    public PictureColorspace Colorspace => new(ColorMatrix, ColorPrimaries, ColorTrc, ColorRange);

    /// <inheritdoc cref="AVCodecParameters.video_delay" />
    public int VideoDelay => Handle.Ref.video_delay;

    //Audio fields

    /// <inheritdoc cref="AVCodecParameters.sample_rate" />
    public int SampleRate => Handle.Ref.sample_rate;

    /// <inheritdoc cref="AVCodecParameters.block_align" />
    public int BlockAlign => Handle.Ref.block_align;

    /// <inheritdoc cref="AVCodecParameters.frame_size" />
    public int FrameSize => Handle.Ref.frame_size;

    /// <inheritdoc cref="AVCodecParameters.initial_padding" />
    public int InitialPaddingSamples => Handle.Ref.initial_padding;

    /// <inheritdoc cref="AVCodecParameters.trailing_padding" />
    public int TrailingPaddingSamples => Handle.Ref.trailing_padding;

    /// <inheritdoc cref="AVCodecParameters.seek_preroll" />
    public int SeekPrerollSamples => Handle.Ref.seek_preroll;

    /// <inheritdoc cref="AVCodecParameters.ch_layout" />
    public ChannelLayout ChannelLayout => new(Handle.Ref.ch_layout);

    public int NumChannels => Handle.Ref.ch_layout.nb_channels;
    public AVSampleFormat SampleFormat => (AVSampleFormat)Handle.Ref.format;

    public AudioFormat AudioFormat => new(SampleFormat, SampleRate, ChannelLayout);

    /// <inheritdoc cref="AVCodecParameters.coded_side_data"/>
    public PacketSideDataList CodedSideData {
        get {
            unsafe {
                var raw = Handle.Raw;
                
                return new PacketSideDataList(
                    &raw->coded_side_data,
                    &raw->nb_coded_side_data);
            }
        }
    }

    public bool Equals(MediaCodecParameters other)
    {
        return Handle == other.Handle;
    }
    
    protected override unsafe void Free()
    {
        fixed (AVCodecParameters** ptr = &_handle) {
            ffmpeg.avcodec_parameters_free(ptr);
        }
    }
}