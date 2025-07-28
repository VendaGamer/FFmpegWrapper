namespace FFmpegWrapper.Containers;

/// <summary>
/// Flags for AVOutputFormat that control various aspects of muxing behavior
/// </summary>
[Flags]
public enum OutputFormatFlags
{
    /// <summary>
    /// No special flags
    /// </summary>
    None = 0,
    
    /// <summary>
    /// Demuxer will use avio_open, no opened file should be provided by the caller
    /// </summary>
    NoFile = ffmpeg.AVFMT_NOFILE,
    
    /// <summary>
    /// Needs '%d' in filename
    /// </summary>
    NeedNumber = ffmpeg.AVFMT_NEEDNUMBER,
    
    /// <summary>
    /// Show format stream IDs numbers
    /// </summary>
    ShowIds = ffmpeg.AVFMT_SHOW_IDS,
    
    /// <summary>
    /// Format wants global header
    /// </summary>
    GlobalHeader = ffmpeg.AVFMT_GLOBALHEADER,
    
    /// <summary>
    /// Format does not need / have any timestamps
    /// </summary>
    NoTimestamps = ffmpeg.AVFMT_NOTIMESTAMPS,
    
    /// <summary>
    /// Use generic index building code
    /// </summary>
    GenericIndex = ffmpeg.AVFMT_GENERIC_INDEX,
    
    /// <summary>
    /// Format allows timestamp discontinuities. Note, muxers always require valid (monotone) timestamps
    /// </summary>
    TsDiscont = ffmpeg.AVFMT_TS_DISCONT,
    
    /// <summary>
    /// Format allows variable fps
    /// </summary>
    VariableFps = ffmpeg.AVFMT_VARIABLE_FPS,
    
    /// <summary>
    /// Format does not need width/height
    /// </summary>
    NoDimensions = ffmpeg.AVFMT_NODIMENSIONS,
    
    /// <summary>
    /// Format does not require any streams
    /// </summary>
    NoStreams = ffmpeg.AVFMT_NOSTREAMS,
    
    /// <summary>
    /// Format does not allow to fall back on binary search via read_timestamp
    /// </summary>
    NoBinSearch = ffmpeg.AVFMT_NOBINSEARCH,
    
    /// <summary>
    /// Format does not allow to fall back on generic search
    /// </summary>
    NoGenSearch = ffmpeg.AVFMT_NOGENSEARCH,
    
    /// <summary>
    /// Format does not allow seeking by bytes
    /// </summary>
    NoByteSeek = ffmpeg.AVFMT_NO_BYTE_SEEK,
    
    /// <summary>
    /// Format allows flushing. If not set, the muxer will not receive a NULL packet in the write_packet function
    /// </summary>
    AllowFlush = ffmpeg.AVFMT_ALLOW_FLUSH,
    
    /// <summary>
    /// Format does not require strictly increasing timestamps, but they must still be monotonic
    /// </summary>
    TsNonstrict = ffmpeg.AVFMT_TS_NONSTRICT,
    
    /// <summary>
    /// Format allows muxing negative timestamps. If not set the timestamp will be shifted in av_write_frame and av_interleaved_write_frame so they start from 0.
    /// The user or muxer can override this through AVFormatContext.avoid_negative_ts
    /// </summary>
    TsNegative = ffmpeg.AVFMT_TS_NEGATIVE,
    
    /// <summary>
    /// Seeking is based on PTS
    /// </summary>
    SeekToPts = ffmpeg.AVFMT_SEEK_TO_PTS
}

/// <summary>
/// Extension methods for OutputFormatFlags to provide additional functionality
/// </summary>
public static class OutputFormatFlagsExtensions
{
    /// <summary>
    /// Checks if the format supports seeking
    /// </summary>
    public static bool SupportsSeek(this OutputFormatFlags flags)
    {
        return !flags.HasFlag(OutputFormatFlags.NoBinSearch) && 
               !flags.HasFlag(OutputFormatFlags.NoGenSearch) && 
               !flags.HasFlag(OutputFormatFlags.NoByteSeek);
    }
    
    /// <summary>
    /// Checks if the format requires external file handling
    /// </summary>
    public static bool RequiresExternalFile(this OutputFormatFlags flags)
    {
        return !flags.HasFlag(OutputFormatFlags.NoFile);
    }
    
    /// <summary>
    /// Checks if the format supports variable frame rates
    /// </summary>
    public static bool SupportsVariableFps(this OutputFormatFlags flags)
    {
        return flags.HasFlag(OutputFormatFlags.VariableFps);
    }
    
    /// <summary>
    /// Checks if the format requires numbered filenames (like image sequences)
    /// </summary>
    public static bool RequiresNumberedFiles(this OutputFormatFlags flags)
    {
        return flags.HasFlag(OutputFormatFlags.NeedNumber);
    }
    
    /// <summary>
    /// Checks if the format supports flushing operations
    /// </summary>
    public static bool SupportsFlush(this OutputFormatFlags flags)
    {
        return flags.HasFlag(OutputFormatFlags.AllowFlush);
    }
    
    /// <summary>
    /// Checks if the format requires a global header
    /// </summary>
    public static bool RequiresGlobalHeader(this OutputFormatFlags flags)
    {
        return flags.HasFlag(OutputFormatFlags.GlobalHeader);
    }
    
    /// <summary>
    /// Checks if the format works with timestamps
    /// </summary>
    public static bool WorksWithTimestamps(this OutputFormatFlags flags)
    {
        return !flags.HasFlag(OutputFormatFlags.NoTimestamps);
    }
    
    /// <summary>
    /// Checks if the format supports negative timestamps
    /// </summary>
    public static bool SupportsNegativeTimestamps(this OutputFormatFlags flags)
    {
        return flags.HasFlag(OutputFormatFlags.TsNegative);
    }
}