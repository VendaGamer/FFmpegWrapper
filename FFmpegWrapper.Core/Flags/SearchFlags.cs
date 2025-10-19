namespace FFmpegWrapper.Core.Flags;

[Flags]
public enum SearchFlags
{
    /// <summary>
    /// No special search behavior
    /// </summary>
    None = 0,
    
    /// <summary>
    /// Search in children objects
    /// </summary>
    Children = ffmpeg.AV_OPT_SEARCH_CHILDREN,
    
    /// <summary>
    /// Search in fake/dummy objects
    /// </summary>
    /// <remarks>
    ///  The obj passed to av_opt_find() is fake - only a double pointer to AVClass
    ///  instead of a required pointer to a struct containing AVClass. This is
    ///  useful for searching for options without needing to allocate the corresponding
    ///  object.
    /// </remarks>
    Dummy = ffmpeg.AV_OPT_SEARCH_FAKE_OBJ,
}