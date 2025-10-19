namespace FFmpegWrapper.Core;

public enum FFmpegLogLevel
{
    /// <summary> Print no output. </summary>
    Quiet = ffmpeg.AV_LOG_QUIET,

    /// <summary> Something went really wrong and we will crash now. </summary>
    Panic = ffmpeg.AV_LOG_PANIC,

    /// <summary> Something went wrong and recovery is not possible.
    /// For example, no header was found for a format which depends
    /// on headers or an illegal combination of parameters is used.
    /// </summary>
    Fatal = ffmpeg.AV_LOG_FATAL,

    /// <summary> Something went wrong and cannot losslessly be recovered.
    /// However, not all future data is affected.
    /// </summary>
    Error = ffmpeg.AV_LOG_ERROR,

    /// <summary> Something somehow does not look correct. This may or may not
    /// lead to problems. An example would be the use of '-vstrict -2'.
    /// </summary>
    Warning = ffmpeg.AV_LOG_WARNING,

    /// <summary> Standard information. </summary>
    Info = ffmpeg.AV_LOG_INFO,

    /// <summary> Detailed information. </summary>
    Verbose = ffmpeg.AV_LOG_VERBOSE,

    /// <summary> Stuff which is only useful for libav* developers. </summary>
    Debug = ffmpeg.AV_LOG_DEBUG,

    /// <summary> Extremely verbose debugging, useful for libav* development. </summary> 
    Trace = ffmpeg.AV_LOG_TRACE,
}