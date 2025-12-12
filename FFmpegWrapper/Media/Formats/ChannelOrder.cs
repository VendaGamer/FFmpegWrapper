namespace FFmpegWrapper.Media.Formats;

public enum ChannelOrder
{
    /// <summary>
    /// Only the channel count is specified, without any further information
    /// about the channel order.
    /// </summary>
    Unspecified = AVChannelOrder.AV_CHANNEL_ORDER_UNSPEC,
    /// <summary>
    /// The native channel order, i.e. the channels are in the same order in
    /// which they are defined in the AVChannel enum. This supports up to 63
    /// different channels.
    /// </summary>
    Native = AVChannelOrder.AV_CHANNEL_ORDER_NATIVE,
    /// <summary>
    /// The channel order does not correspond to any other predefined order and
    /// is stored as an explicit map. For example, this could be used to support
    /// layouts with 64 or more channels, or with empty/skipped (AV_CHAN_SILENCE)
    /// channels at arbitrary positions.
    /// </summary>
    Custom = AVChannelOrder.AV_CHANNEL_ORDER_CUSTOM,
    /// <summary>
    /// The audio is represented as the decomposition of the sound field into
    /// spherical harmonics. Each channel corresponds to a single expansion
    /// component. Channels are ordered according to ACN (Ambisonic Channel
    /// Number).
    /// <para/>
    /// The channel with the index n in the stream contains the spherical
    /// harmonic of degree l and order m given by
    /// <code>
    ///   l   = floor(sqrt(n)),
    ///   m   = n - l * (l + 1).
    /// </code>
    /// <para/>
    /// Conversely given a spherical harmonic of degree l and order m, the
    /// corresponding channel index n is given by
    /// <code>
    ///   n = l * (l + 1) + m.
    /// </code>
    /// <para/>
    /// Normalization is assumed to be SN3D (Schmidt Semi-Normalization)
    /// as defined in AmbiX format $ 2.1.
    /// </summary>
    Ambisonic = AVChannelOrder.AV_CHANNEL_ORDER_AMBISONIC,
}
