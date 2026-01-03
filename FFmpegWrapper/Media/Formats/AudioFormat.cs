namespace FFmpegWrapper.Media.Formats;

public readonly struct AudioFormat : IEquatable<AudioFormat>
{
    public readonly AVSampleFormat SampleFormat;
    public readonly int SampleRate;
    public readonly ChannelLayout Layout;
    /// <summary>
    /// Gets the number of audio channels configured for AudioFormat.
    /// This value determines the channel layout (e.g., 1 for mono, 2 for stereo, 6 for 5.1 surround).
    /// </summary>
    /// <value>The number of audio channels, typically ranging from 1 to 8 or more.</value>
    public int NumChannels => Layout.NumChannels;
    public int BytesPerSample => av_get_bytes_per_sample(SampleFormat);
    public bool IsPlanar => av_sample_fmt_is_planar(SampleFormat) is not 0;

    public AudioFormat(AVSampleFormat sampleFmt, int sampleRate, int numChannels)
    {
        SampleFormat = sampleFmt;
        SampleRate = sampleRate;
        Layout = ChannelLayout.GetDefault(numChannels);
    }

    public AudioFormat(AVSampleFormat sampleFmt, int sampleRate, AVChannelLayout channelLayout)
        : this(sampleFmt, sampleRate, new ChannelLayout(channelLayout)) { }
    
    public AudioFormat(AVSampleFormat sampleFmt, int sampleRate, ChannelLayout channelLayout)
    {
        SampleFormat = sampleFmt;
        SampleRate = sampleRate;
        Layout = channelLayout;
    }

    public override string ToString()
    {
        var fmt = SampleFormat.ToString().Substring("AV_SAMPLE_FMT_".Length);
        return $"{SampleRate} Hz, {Layout}, {fmt}";
    }

    public bool Equals(AudioFormat other)
        => other.SampleFormat == SampleFormat && other.SampleRate == SampleRate && other.Layout.Equals(Layout);

    public override bool Equals(object? obj) => obj is AudioFormat other && Equals(other);
    public override int GetHashCode() => (SampleRate, NumChannels, (int)SampleFormat).GetHashCode();
}
