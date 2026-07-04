namespace FFmpegMiniAudioInterop.Extensions;

using FFmpegBindings.Abstractions;
using MiniAudioBindings.Abstractions;

public static class EnumExtensions
{
    extension(AVSampleFormat sampleFormat)
    {
        public ma_format ToMaFormat()
            => sampleFormat switch {
                AVSampleFormat.AV_SAMPLE_FMT_U8 or AVSampleFormat.AV_SAMPLE_FMT_U8P => ma_format.ma_format_u8,
                AVSampleFormat.AV_SAMPLE_FMT_S16 or AVSampleFormat.AV_SAMPLE_FMT_S16P => ma_format.ma_format_s16,
                AVSampleFormat.AV_SAMPLE_FMT_S32 or AVSampleFormat.AV_SAMPLE_FMT_S32P => ma_format.ma_format_s32,
                AVSampleFormat.AV_SAMPLE_FMT_FLT or AVSampleFormat.AV_SAMPLE_FMT_FLTP => ma_format.ma_format_f32,
                _ => ma_format.ma_format_unknown
            };
    }
}