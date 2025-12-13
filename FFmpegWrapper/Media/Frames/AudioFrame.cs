namespace FFmpegWrapper.Media.Frames;

public class AudioFrame : MediaFrame
{
    public AVSampleFormat SampleFormat => (AVSampleFormat)Handle.Ref.format;
    public int SampleRate => Handle.Ref.sample_rate;
    public ChannelLayout ChannelLayout => new(Handle.Ref.ch_layout);

    public AudioFormat Format => new(SampleFormat, SampleRate, ChannelLayout);

    public unsafe byte** Data => (byte**)&_handle->data;
    public ReadOnlySpan<int> LineSize {
        get {
            unsafe
            {
                return new ReadOnlySpan<int>(Handle.Raw->linesize, AV_NUM_DATA_POINTERS);
            }
        }
    }

    public int Stride {
        get {
            unsafe
            {
                return Handle.Ref.linesize[0];
            }
        }
    }


    public bool IsPlanar => av_sample_fmt_is_planar(SampleFormat) is not 0;
    /// <summary>
    /// 
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Is thrown if value is less then zero or if value is greater than Capacity</exception>
    public int Count {
        get => Handle.Ref.nb_samples;
        set {
            if (value < 0 || value > Capacity) {
                throw new ArgumentOutOfRangeException(nameof(value), "Must must be positive and not exceed the frame capacity.");
            }
            Handle.Ref.nb_samples = value;
        }
    }

    public int Capacity {
        get {
            unsafe
            {
                ref var handle = ref Handle.Ref;
            
                return handle.linesize[0] / 
                       av_get_bytes_per_sample(SampleFormat)
                       * (IsPlanar ? 1 : handle.ch_layout.nb_channels);
            }
        }
    }

    public AudioFrame(in AudioFormat fmt, int capacity)
    {
        unsafe
        {
            _handle = av_frame_alloc();
            _handle->format = (int)fmt.SampleFormat;
            _handle->sample_rate = fmt.SampleRate;
            fmt.Layout.CopyTo(&_handle->ch_layout);

            _handle->nb_samples = capacity;
            av_frame_get_buffer(_handle, 0).CheckError("Failed to allocate frame buffers.");
        }
    }
    public AudioFrame(AVSampleFormat fmt, int sampleRate, int numChannels, int capacity)
        : this(new AudioFormat(fmt, sampleRate, numChannels), capacity) { }
    
    public AudioFrame(FFHandle<AVFrame> frameHandle)
    {
        unsafe
        {
            _handle = frameHandle;
        }
    }

    public Span<T> GetSamples<T>(int channel = 0) where T : unmanaged
    {
        unsafe
        {
            if ((uint)channel >= (uint)ChannelLayout.NumChannels || (!IsPlanar && channel != 0)) {
                throw new ArgumentOutOfRangeException();
            }
            return new Span<T>(Data[channel], Stride / sizeof(T));
        }
    }

    /// <summary> Copy interleaved samples from the span into this frame. </summary>
    /// <returns> Returns the number of samples copied. </returns>
    public int CopyFrom(Span<float> samples) => CopyFrom<float>(samples);

    /// <inheritdoc cref="CopyFrom(Span{float})"/>
    public int CopyFrom(Span<short> samples) => CopyFrom<short>(samples);

    public int CopyFrom(Span<byte> samples) => CopyFrom<byte>(samples);

    private int CopyFrom<T>(Span<T> samples) where T : unmanaged
    {
        unsafe {
         
            var fmt = Format;
            if (fmt.IsPlanar || fmt.BytesPerSample != sizeof(T)) {
                throw new InvalidOperationException("Incompatible format");
            }
            if (samples.Length % fmt.NumChannels != 0) {
                throw new ArgumentException("Sample count must be a multiple of channel count.", nameof(samples));
            }

            int count = Math.Min(Capacity, samples.Length / fmt.NumChannels);

            fixed (T* ptr = samples) {
                byte** temp = null;
                av_samples_copy(_handle->extended_data, temp, 0, 0, count, fmt.NumChannels, fmt.SampleFormat);
            }
            return count;
            
        }
    }
}
