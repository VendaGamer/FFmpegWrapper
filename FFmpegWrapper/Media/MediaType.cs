namespace FFmpegWrapper.Media;

/// <summary>
/// Represents the different media types handled by FFmpeg.
/// </summary>
public enum MediaType
{
    /// <summary>
    /// Unknown media type (default or unclassified).
    /// </summary>
    Unknown = AVMediaType.AVMEDIA_TYPE_UNKNOWN,

    /// <summary>
    /// Video stream (e.g., H.264, VP9).
    /// </summary>
    Video = AVMediaType.AVMEDIA_TYPE_VIDEO,

    /// <summary>
    /// Audio stream (e.g., AAC, MP3).
    /// </summary>
    Audio = AVMediaType.AVMEDIA_TYPE_AUDIO,

    /// <summary>
    /// Data stream (e.g., timed metadata).
    /// Rare and usually not directly played back.
    /// </summary>
    Data = AVMediaType.AVMEDIA_TYPE_DATA,

    /// <summary>
    /// Subtitle stream (e.g., SRT, ASS).
    /// </summary>
    Subtitle = AVMediaType.AVMEDIA_TYPE_SUBTITLE,

    /// <summary>
    /// Attachment stream (e.g., fonts or images used in subtitles).
    /// </summary>
    Attachment = AVMediaType.AVMEDIA_TYPE_ATTACHMENT,

    /// <summary>
    /// Represents the number of media types; not a media type itself.
    /// Useful for iteration or bounds checking.
    /// </summary>
    Count = AVMediaType.AVMEDIA_TYPE_NB
}