namespace FFmpegWrapper.Hardware;

/// <summary> Flags to apply to hardware frame memory mappings. </summary>
[Flags]
public enum HardwareFrameTransferDirection : byte
{
    /// <summary> Transfer the data from the queried hw frame. </summary>
    From = AVHWFrameTransferDirection.AV_HWFRAME_TRANSFER_DIRECTION_FROM,
    /// <summary> Transfer the data to the queried hw frame. </summary>
    To = AVHWFrameTransferDirection.AV_HWFRAME_TRANSFER_DIRECTION_TO,
}
