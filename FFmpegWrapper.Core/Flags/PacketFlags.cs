namespace FFmpegWrapper.Core.Flags;

[Flags]
public enum PacketFlags
{
    /// <summary>
    /// No special flags are set.
    /// </summary>
    None = 0,

    /// <summary>
    /// The packet contains a keyframe — a frame that can be decoded independently.
    /// This flag is typically set for I-frames in video streams.
    /// </summary>
    Key = ffmpeg.AV_PKT_FLAG_KEY,

    /// <summary>
    /// The packet is possibly corrupted.
    /// This flag is set when FFmpeg detects data integrity issues during demuxing.
    /// The data may be partially invalid or incomplete.
    /// </summary>
    Corrupt = ffmpeg.AV_PKT_FLAG_CORRUPT,

    /// <summary>
    /// The packet should be discarded without processing.
    /// This is used for packets that are known to be invalid or unnecessary.
    /// </summary>
    Discard = ffmpeg.AV_PKT_FLAG_DISCARD,

    /// <summary>
    /// The packet comes from a trusted source.
    /// This flag is used internally by FFmpeg demuxers and filters to mark packets
    /// that have passed verification or originate from a reliable container.
    /// </summary>
    Trusted = ffmpeg.AV_PKT_FLAG_TRUSTED,

    /// <summary>
    /// The packet contains data that can be dropped if necessary (e.g., for bitrate control).
    /// Typically used for “disposable” frames in codecs that support them (like B-frames).
    /// </summary>
    Disposable = ffmpeg.AV_PKT_FLAG_DISPOSABLE,
}