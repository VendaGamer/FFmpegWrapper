namespace FFmpegWrapper.Media.Streams;

using Core;

using Parameters;

public readonly struct MediaStream
{
    public Handle<AVStream> Handle {
        get {
            unsafe
            {
                return (Handle<AVStream>)_handle;
            }
        }
    }
    
    public int Index => Handle.Ref.index;

    /// <inheritdoc cref="AVStream.time_base" />
    public Rational TimeBase => Handle.Ref.time_base;

    /// <summary> Pts of the first frame of the stream in presentation order, in stream time base. </summary>
    public long? StartTime {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Handle.Ref.start_time;
    }

    /// <inheritdoc cref="AVStream.duration" />
    public TimeSpan? Duration {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Rational.GetTimeSpan(Handle.Ref.duration, TimeBase);
    }

    /// <inheritdoc cref="AVStream.avg_frame_rate" />
    public Rational AvgFrameRate => Handle.Ref.avg_frame_rate;

    /// <inheritdoc cref="AVStream.r_frame_rate" />
    public Rational RealFrameRate => Handle.Ref.r_frame_rate;

    public ObservedMediaDictionary Metadata {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            unsafe
            {
                return new ObservedMediaDictionary(&Handle.Raw->metadata);
            }
        }
    }

    /// <inheritdoc cref="AVStream.disposition" />
    public AVDispositionFlags Disposition => (AVDispositionFlags)Handle.Ref.disposition;

    /// <inheritdoc cref="AVStream.codecpar" />
    public CodecParameters CodecPars {
        get {
            unsafe
            {
                return new CodecParameters((Handle<AVCodecParameters>)Handle.Ref.codecpar);
            }
        }
    }
    
    /// <summary> Returns the corresponding <see cref="TimeSpan"/> for the given timestamp based on <see cref="TimeBase"/> units. </summary>
    public TimeSpan? GetTimestamp(long pts) => Rational.GetTimeSpan(pts, TimeBase);
    
    internal unsafe readonly AVStream* _handle;

    public MediaStream(Handle<AVStream> stream)
    {
        unsafe
        {
            _handle = stream;
        }
    }
}
