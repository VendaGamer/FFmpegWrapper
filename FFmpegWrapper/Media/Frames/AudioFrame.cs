﻿namespace FFmpeg.Wrapper;

public unsafe class AudioFrame : MediaFrame
{
    public AVSampleFormat SampleFormat => (AVSampleFormat)_handle->format;
    public int SampleRate => _handle->sample_rate;
    public int NumChannels => _handle->ch_layout.nb_channels;
    public ChannelLayout ChannelLayout => ChannelLayout.FromExisting(&_handle->ch_layout);

    public AudioFormat Format => new(SampleFormat, SampleRate, ChannelLayout);

    public byte** Data => (byte**)&_handle->data;
    public int Stride => _handle->linesize[0];

    public bool IsPlanar => ffmpeg.av_sample_fmt_is_planar(SampleFormat) != 0;
    /// <summary>
    /// 
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Is thrown if value is less then zero or if value is greater than Capacity</exception>
    public int Count {
        get => _handle->nb_samples;
        set {
            if (value < 0 || value > Capacity) {
                throw new ArgumentOutOfRangeException(nameof(value), "Must must be positive and not exceed the frame capacity.");
            }
            _handle->nb_samples = value;
        }
    }

    public int Capacity => Stride / (ffmpeg.av_get_bytes_per_sample(SampleFormat) * (IsPlanar ? 1 : NumChannels));

    /// <summary> Allocates an empty <see cref="AVFrame"/>. </summary>
    public AudioFrame()
        : this(ffmpeg.av_frame_alloc(), takeOwnership: true) { }

    public AudioFrame(in AudioFormat fmt, int capacity)
    {
        _handle = ffmpeg.av_frame_alloc();
        _handle->format = (int)fmt.SampleFormat;
        _handle->sample_rate = fmt.SampleRate;
        fmt.Layout.CopyTo(&_handle->ch_layout);

        _handle->nb_samples = capacity;
        ffmpeg.av_frame_get_buffer(_handle, 0).CheckError("Failed to allocate frame buffers.");
    }
    public AudioFrame(AVSampleFormat fmt, int sampleRate, int numChannels, int capacity)
        : this(new AudioFormat(fmt, sampleRate, numChannels), capacity) { }

    /// <summary> Wraps an existing <see cref="AVFrame"/> into an <see cref="AudioFrame"/> instance. </summary>
    /// <param name="takeOwnership">True if <paramref name="frame"/> should be freed when Dispose() is called.</param>
    public AudioFrame(AVFrame* frame, bool takeOwnership)
    {
        if (frame == null) {
            throw new ArgumentNullException(nameof(frame));
        }
        _handle = frame;
        _ownsFrame = takeOwnership;
    }

    public Span<T> GetSamples<T>(int channel = 0) where T : unmanaged
    {
        if ((uint)channel >= (uint)NumChannels || (!IsPlanar && channel != 0)) {
            throw new ArgumentOutOfRangeException();
        }
        return new Span<T>(Data[channel], Stride / sizeof(T));
    }

    /// <summary> Copy interleaved samples from the span into this frame. </summary>
    /// <returns> Returns the number of samples copied. </returns>
    public int CopyFrom(Span<float> samples) => CopyFrom<float>(samples);

    /// <inheritdoc cref="CopyFrom(Span{float})"/>
    public int CopyFrom(Span<short> samples) => CopyFrom<short>(samples);

    private int CopyFrom<T>(Span<T> samples) where T : unmanaged
    {
        var fmt = Format;
        if (fmt.IsPlanar || fmt.BytesPerSample != sizeof(T)) {
            throw new InvalidOperationException("Incompatible format");
        }
        if (samples.Length % fmt.NumChannels != 0) {
            throw new ArgumentException("Sample count must be a multiple of channel count.", nameof(samples));
        }

        int count = Math.Min(Capacity, samples.Length / fmt.NumChannels);

        fixed (T* ptr = samples) {
            byte** temp = stackalloc byte*[1] { (byte*)ptr };
            ffmpeg.av_samples_copy(_handle->extended_data, temp, 0, 0, count, fmt.NumChannels, fmt.SampleFormat);
        }
        return count;
    }
}