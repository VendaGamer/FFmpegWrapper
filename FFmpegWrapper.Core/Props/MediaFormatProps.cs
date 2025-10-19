namespace FFmpegWrapper.Core.Props;

public enum MediaFormatProps
{
    Experimental = ffmpeg.AVFMT_EXPERIMENTAL,
    AvoidNegativeTimestampsAuto = ffmpeg.AVFMT_AVOID_NEG_TS_AUTO,
    AvoidNegativeTimestampsDisabled = ffmpeg.AVFMT_AVOID_NEG_TS_DISABLED,
    AvoidNegativeTimestamps = ffmpeg.AVFMT_AVOID_NEG_TS_MAKE_NON_NEGATIVE,
}