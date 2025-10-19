namespace FFmpegWrapper.Media.Frames;

public unsafe abstract class MediaFrame : FFObject<AVFrame>
{
    /// <inheritdoc cref="AVFrame.best_effort_timestamp" />
    public long? BestEffortTimestamp => _handle->best_effort_timestamp;

    /// <inheritdoc cref="AVFrame.pts" />
    public long? PresentationTimestamp {
        get => FFHelper.GetPTS(_handle->pts);
        set => FFHelper.SetPTS(ref _handle->pts, value);
    }

    /// <summary> Duration of the frame, in the same units as <see cref="PresentationTimestamp"/>. Null if unknown. </summary>
    public long? Duration {
        get => _handle->duration > 0 ? _handle->duration : null;
        set => _handle->duration = value ?? 0;
    }

    /// <inheritdoc cref="AVFrame.side_data"/>
    public FrameSideDataList SideData => new(_handle);

    protected override void Free()
    {
        if (_handle is not null) {
            fixed (AVFrame** ppFrame = &_handle) {
                ffmpeg.av_frame_free(ppFrame);
            }
        }
    }
}