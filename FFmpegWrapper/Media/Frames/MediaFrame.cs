namespace FFmpegWrapper.Media.Frames;

using Abstractions;

public abstract class MediaFrame : FFObject<AVFrame>
{
    /// <inheritdoc cref="AVFrame.best_effort_timestamp" />
    public long? BestEffortTimestamp {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            unsafe
            {
                return Handle.Raw->best_effort_timestamp;
            }
        }
    }

    /// <inheritdoc cref="AVFrame.pts" />
    public long? PresentationTimestamp {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            unsafe
            {
                return FFHelper.GetPts(Handle.Raw->pts);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set {
            unsafe
            {
                FFHelper.SetPts(ref Handle.Raw->pts, value);
            }
        }
    }

    /// <summary> Duration of the frame, in the same units as <see cref="PresentationTimestamp"/>. Null if unknown. </summary>
    public long? Duration {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            unsafe
            {
                var handle = Handle.Raw;
                
                return handle->duration is 0 ? Handle.Raw->duration : null;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set {
            unsafe {
                var handle = Handle.Raw;

                if (value is null)
                    handle->duration = 0;
                else
                    handle->duration = value.Value;
            }
        }
    }

    /// <inheritdoc cref="AVFrame.side_data"/>
    public FrameSideDataList SideData {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            unsafe
            {
                return new FrameSideDataList(_handle);
            }
        }
    }

    public ReadOnlySpan<int> LineSize {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            unsafe {
                return new ReadOnlySpan<int>(Handle.Raw->linesize, AV_NUM_DATA_POINTERS);
            }
        }
    }

    protected override unsafe void Free()
    {
        if (_handle is not null) {
            fixed (AVFrame** ppFrame = &_handle) {
                av_frame_free(ppFrame);
            }
        }
    }
}
