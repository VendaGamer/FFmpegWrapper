namespace FFmpegWrapper.Media;

/// <summary>
/// Flags for controlling AVDictionary operations
/// </summary>
[Flags]
public enum MediaDictionaryFlags
{
    /// <summary>
    /// Default behavior - no special flags
    /// </summary>
    None = 0,
    
    /// <summary>
    /// Case-sensitive key matching
    /// </summary>
    MatchCase = ffmpeg.AV_DICT_MATCH_CASE,
    
    /// <summary>
    /// Match key as a prefix (ignore suffix)
    /// </summary>
    IgnoreSuffix = ffmpeg.AV_DICT_IGNORE_SUFFIX,
    
    /// <summary>
    /// Don't overwrite existing entries
    /// </summary>
    DontOverwrite = ffmpeg.AV_DICT_DONT_OVERWRITE,
    
    /// <summary>
    /// Append value to existing entry instead of replacing
    /// </summary>
    Append = ffmpeg.AV_DICT_APPEND,
    
    /// <summary>
    /// Allow duplicate keys (multikey)
    /// </summary>
    MultiKey = ffmpeg.AV_DICT_MULTIKEY,
    
    /// <summary>
    /// Don't strdup key - key must remain valid for dictionary lifetime
    /// </summary>
    DontStrdupKey = ffmpeg.AV_DICT_DONT_STRDUP_KEY,
    
    /// <summary>
    /// Don't strdup value - value must remain valid for dictionary lifetime
    /// </summary>
    DontStrdupVal = ffmpeg.AV_DICT_DONT_STRDUP_VAL
}