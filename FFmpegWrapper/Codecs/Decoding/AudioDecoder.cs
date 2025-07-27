namespace FFmpegWrapper.Codecs.Decoding;

public unsafe class AudioDecoder : MediaDecoder
{
    public AVSampleFormat SampleFormat => handle->sample_fmt;
    public int SampleRate => handle->sample_rate;
    public int NumChannels => handle->ch_layout.nb_channels;
    public ChannelLayout ChannelLayout => ChannelLayout.FromExisting(&handle->ch_layout);

    public AudioFormat Format => new(SampleFormat, SampleRate, ChannelLayout);

    public AudioDecoder(AVCodecID codecId)
        : this(MediaCodec.GetDecoder(codecId)) { }

    public AudioDecoder(MediaCodec codec)
        : this(AllocContext(codec), takeOwnership: true) { }

    public AudioDecoder(AVCodecContext* ctx, bool takeOwnership)
        : base(ctx, MediaTypes.Audio, takeOwnership) { }
}