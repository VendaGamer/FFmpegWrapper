namespace FFmpegWrapper.Core.Flags;

[Flags]
public enum InterpolationMode
{
    None = 0,
    FastBilinear    = ffmpeg.SWS_FAST_BILINEAR,
    Bilinear        = ffmpeg.SWS_BILINEAR,
    Bicubic         = ffmpeg.SWS_BICUBIC,
    NearestNeighbor = ffmpeg.SWS_POINT,
    Box             = ffmpeg.SWS_AREA,
    Gaussian        = ffmpeg.SWS_GAUSS,
    Sinc            = ffmpeg.SWS_SINC,
    Lanczos         = ffmpeg.SWS_LANCZOS,
    Spline          = ffmpeg.SWS_SPLINE,

    /// <summary> Flag: Prioritize quality over speed. This sets ACCURATE_RND, BITEXACT, and FULL_CHR_H_INT. </summary>
    /// <remarks> See https://stackoverflow.com/a/70894724 for details on the meaning of these flags. </remarks>
    HighQuality     = ffmpeg.SWS_ACCURATE_RND | ffmpeg.SWS_BITEXACT | ffmpeg.SWS_FULL_CHR_H_INT,

    /// <summary> Flag: Always interpolate chroma channels when upsampling. </summary>
    InterpolateChroma = ffmpeg.SWS_FULL_CHR_H_INT,
}