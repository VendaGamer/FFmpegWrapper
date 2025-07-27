namespace FFmpegWrapper.Media.Frames;

public unsafe abstract class MediaFrame : FFObject<AVFrame>
{
    protected bool _ownsFrame = true;
    /// <inheritdoc cref="AVFrame.best_effort_timestamp" />
    public long? BestEffortTimestamp => Helpers.GetPTS(handle->best_effort_timestamp);

    /// <inheritdoc cref="AVFrame.pts" />
    public long? PresentationTimestamp {
        get => Helpers.GetPTS(handle->pts);
        set => Helpers.SetPTS(ref handle->pts, value);
    }

    /// <summary> Duration of the frame, in the same units as <see cref="PresentationTimestamp"/>. Null if unknown. </summary>
    public long? Duration {
        get => handle->duration > 0 ? handle->duration : null;
        set => handle->duration = value ?? 0;
    }

    /// <inheritdoc cref="AVFrame.side_data"/>
    public FrameSideDataList SideData => new(handle);

    protected override void Free()
    {
        if (handle != null && _ownsFrame) {
            fixed (AVFrame** ppFrame = &handle) {
                ffmpeg.av_frame_free(ppFrame);
            }
        }
        handle = null;
    }
}