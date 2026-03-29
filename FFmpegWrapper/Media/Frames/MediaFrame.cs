namespace FFmpegWrapper.Media.Frames;

public abstract class MediaFrame : FFObject<AVFrame>
{
    /// <inheritdoc cref="AVFrame.best_effort_timestamp" />
    public long? BestEffortTimestamp {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Handle.Ref.best_effort_timestamp;
    }

    /// <inheritdoc cref="AVFrame.pts" />
    public long? PresentationTimestamp {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => FFHelper.GetPts(Handle.Ref.pts);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => FFHelper.SetPts(ref Handle.Ref.pts, value);
    }

    /// <summary> Duration of the frame, in the same units as <see cref="PresentationTimestamp"/>. Null if unknown. </summary>
    public long? Duration {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            var duration = Handle.Ref.duration;
                
            return duration is 0 ? duration : null;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => Handle.Ref.duration = value ?? 0;
    }

    /// <inheritdoc cref="AVFrame.side_data"/>
    public FrameSideDataList SideData {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(Handle);
    }

    public ReadOnlySpan<int> LineSize {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            unsafe {
                return new ReadOnlySpan<int>(&Handle.Raw->linesize._0, AV_NUM_DATA_POINTERS);
            }
        }
    }

    public unsafe byte** Data {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => &_handle->data._0;
    }

    public int Stride {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Handle.Ref.linesize[0];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MediaFrame CreateFromType(AVMediaType type)
        => type switch {
            AVMediaType.AVMEDIA_TYPE_VIDEO => new VideoFrame(),
            AVMediaType.AVMEDIA_TYPE_AUDIO => new AudioFrame(),
            _ => throw new ArgumentException("Invalid media type.", nameof(type))
        };

    #region Constructors

    protected MediaFrame(Handle<AVFrame> handle) : base(handle) { }

    protected unsafe MediaFrame() : base(av_frame_alloc()) { }

    #endregion
    

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected override unsafe void Free()
    {
        if (_handle is not null) {
            fixed (AVFrame** ppFrame = &_handle) {
                av_frame_free(ppFrame);
            }
        }
    }
}
