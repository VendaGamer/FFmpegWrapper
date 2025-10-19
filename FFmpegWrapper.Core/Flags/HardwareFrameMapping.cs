namespace FFmpegWrapper.Core.Flags;
[Flags]
public enum HardwareFrameMapping
{
    None = 0,
    /// <summary> The mapping must be readable. </summary>
    Read = 1 << 0,
    /// <summary> The mapping must be writeable. </summary>
    Write = 1 << 1,
    /// <summary>
    /// The mapped frame will be overwritten completely in subsequent
    /// operations, so the current frame data need not be loaded.  Any values
    /// which are not overwritten are unspecified.
    /// </summary>
    Overwrite = 1 << 2,
    /// <summary>
    /// The mapping must be direct.  That is, there must not be any copying in
    /// the map or unmap steps.  Note that performance of direct mappings may
    /// be much lower than normal memory.
    /// </summary>
    Direct = 1 << 3,
}