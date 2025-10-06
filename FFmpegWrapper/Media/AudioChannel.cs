namespace FFmpegWrapper.Media;

public enum AudioChannel
{
    None = AVChannel.AV_CHAN_NONE,
    FrontLeft = AVChannel.AV_CHAN_FRONT_LEFT,
    FrontRight = AVChannel.AV_CHAN_FRONT_RIGHT,
    FrontCenter = AVChannel.AV_CHAN_FRONT_CENTER,
    LowFrequency = AVChannel.AV_CHAN_LOW_FREQUENCY,
    BackLeft = AVChannel.AV_CHAN_BACK_LEFT,
    BackRight = AVChannel.AV_CHAN_BACK_RIGHT,
    FrontLeftOfCenter = AVChannel.AV_CHAN_FRONT_LEFT_OF_CENTER,
    FrontRightOfCenter = AVChannel.AV_CHAN_FRONT_RIGHT_OF_CENTER,
    BackCenter = AVChannel.AV_CHAN_BACK_CENTER,
    SideLeft = AVChannel.AV_CHAN_SIDE_LEFT,
    SideRight = AVChannel.AV_CHAN_SIDE_RIGHT,
    TopCenter = AVChannel.AV_CHAN_TOP_CENTER,
    TopFrontLeft = AVChannel.AV_CHAN_TOP_FRONT_LEFT,
    TopFrontCenter = AVChannel.AV_CHAN_TOP_FRONT_CENTER,
    TopFrontRight = AVChannel.AV_CHAN_TOP_FRONT_RIGHT,
    TopBackLeft = AVChannel.AV_CHAN_TOP_BACK_LEFT,
    TopBackCenter = AVChannel.AV_CHAN_TOP_BACK_CENTER,
    TopBackRight = AVChannel.AV_CHAN_TOP_BACK_RIGHT,
    /** = AVChannel./** Stereo downmix. */
    StereoLeft = AVChannel.AV_CHAN_STEREO_LEFT,
    /** = AVChannel./** See above. */
    StereoRight = AVChannel.AV_CHAN_STEREO_RIGHT,
    WideLeft = AVChannel.AV_CHAN_WIDE_LEFT,
    WideRight = AVChannel.AV_CHAN_WIDE_RIGHT,
    SurroundDirectLeft = AVChannel.AV_CHAN_SURROUND_DIRECT_LEFT,
    SurroundDirectRight = AVChannel.AV_CHAN_SURROUND_DIRECT_RIGHT,
    LowFrequency2 = AVChannel.AV_CHAN_LOW_FREQUENCY_2,
    TopSideLeft = AVChannel.AV_CHAN_TOP_SIDE_LEFT,
    TopSideRight = AVChannel.AV_CHAN_TOP_SIDE_RIGHT,
    BottomFrontCenter = AVChannel.AV_CHAN_BOTTOM_FRONT_CENTER,
    BottomFrontLeft = AVChannel.AV_CHAN_BOTTOM_FRONT_LEFT,
    BottomFrontRight = AVChannel.AV_CHAN_BOTTOM_FRONT_RIGHT,

    /// <summary> Channel is empty can be safely skipped. </summary>
    Unused = 0x200,

    /// <summary> Channel contains data, but its position is unknown. </summary>
    Unknown = 0x300,

    /// <summary>
    /// Range of channels between <see cref="AmbisonicBase"/> and
    /// <see cref="AmbisonicEnd"/> represent Ambisonic components using the ACN system.
    /// <para/>
    /// Given a channel id `i` between <see cref="AmbisonicBase"/> and
    /// <see cref="AmbisonicEnd"/> (inclusive), the ACN index of the channel `n` is
    /// `n = i - <see cref="AmbisonicBase"/>`.
    /// </summary>
    /// <remarks>
    /// These values are only used for <see cref="ChannelOrder.Custom"/> channel
    /// orderings, the <see cref="ChannelOrder.Ambisonic"/> ordering orders the channels
    /// implicitly by their position in the stream.
    /// </remarks>
    AmbisonicBase = 0x400,
    // leave space for 1024 ids, which correspond to maximum order-32 harmonics,
    // which should be enough for the foreseeable use cases
    AmbisonicEnd = 0x7ff,
}