namespace FFmpegWrapper.Media;

/// <summary>
/// Type of seeking
/// </summary>
[Flags]
public enum SeekOptions
{
    /// <summary> Seek based on time to the nearest keyframe after or at the requested timestamp. </summary>
    Forward = 0,

    /// <summary> Seek to the nearest keyframe before or at the requested timestamp. </summary>
    Backward = ffmpeg.AVSEEK_FLAG_BACKWARD,

    /// <summary> Allow seeking to non-keyframes. This may cause decoders to fail or output corrupt frames. </summary>
    AllowNonKeyFrames = ffmpeg.AVSEEK_FLAG_ANY,
    
    /// <summary> Seek based on position in bytes </summary>
    Byte = ffmpeg.AVSEEK_FLAG_BYTE,
    
    /// <summary> Seek based on number of frame </summary>
    Frame = ffmpeg.AVSEEK_FLAG_FRAME,
    
}