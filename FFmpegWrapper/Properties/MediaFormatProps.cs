namespace FFmpegWrapper.Flags;

public enum MediaFormatProps
{
    Experimental = ffmpeg.AVFMT_EXPERIMENTAL,
    AllowFlush = ffmpeg.AVFMT_ALLOW_FLUSH,
    AvoidNegativeTimestampsAuto = ffmpeg.AVFMT_AVOID_NEG_TS_AUTO,
    AvoidNegativeTimestampsDisabled = ffmpeg.AVFMT_AVOID_NEG_TS_DISABLED,
    AvoidNegativeTimestamps = ffmpeg.AVFMT_AVOID_NEG_TS_MAKE_NON_NEGATIVE,
    
}