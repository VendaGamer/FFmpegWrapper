namespace FFmpegWrapper.Core.Flags;

[Flags]
public enum DictionaryFlags
{
    None = 0,
    /// <summary>Make case sensitive</summary>
    MatchCase = ffmpeg.AV_DICT_MATCH_CASE,
    /// <summary>Take ownership of a key that has been allocated with <see cref="ffmpeg.av_malloc"/> and children</summary>
    IgnoreSuffix = ffmpeg.AV_DICT_IGNORE_SUFFIX,
    /// <summary>Take ownership of a key that has been allocated with <see cref="ffmpeg.av_malloc"/> and children</summary>
    DontStrdupKey = ffmpeg.AV_DICT_DONT_STRDUP_KEY,
    /// <summary>Take ownership of a value that has been allocated with <see cref="ffmpeg.av_malloc"/> and children</summary>
    DontStrdupValue = ffmpeg.AV_DICT_DONT_STRDUP_VAL,
    /// <summary>Append to entry if such exists</summary>
    DontAppend = ffmpeg.AV_DICT_APPEND,
}