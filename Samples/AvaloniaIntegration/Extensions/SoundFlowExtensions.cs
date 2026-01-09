namespace AvaloniaIntegration.Extensions;

using FFmpegBindings.Abstractions;

using SoundFlow.Enums;
using SoundFlow.Structs;

public static class SoundFlowExtensions
{
    extension(FFmpegWrapper.Media.Formats.AudioFormat format)
    {
        public (AudioFormat format, bool needsConversion) ToSoundFlow()
        {
            var sampleRes = format.SampleFormat.ToSoundFlow();
                
            return (
                new AudioFormat() {
                    Layout = ((AVChannelFlags)format.Layout.Native.u.mask).ToSoundFlow(),
                    Channels = format.Layout.NumChannels,
                    Format = sampleRes.sampleFormat
                },
                sampleRes.needsConversion
            );
        }
    }

    extension(AVSampleFormat format)
    {
        public (SampleFormat sampleFormat, bool needsConversion) ToSoundFlow()
            => format switch {
                AVSampleFormat.AV_SAMPLE_FMT_FLT => (SampleFormat.F32, false),
                AVSampleFormat.AV_SAMPLE_FMT_S32 => (SampleFormat.S32, false),
                AVSampleFormat.AV_SAMPLE_FMT_S16 => (SampleFormat.S16, false),
                AVSampleFormat.AV_SAMPLE_FMT_U8 => (SampleFormat.U8, false),
                _ => (SampleFormat.U8, true),
            };
    }
    
    extension(AVChannelFlags flags)
    {
        public ChannelLayout ToSoundFlow()
            => (ulong)flags switch
        {
            var f when (f & (ulong)AVChannelFlags.AV_CH_LAYOUT_MONO) is (ulong)AVChannelFlags.AV_CH_LAYOUT_MONO =>
                ChannelLayout.Mono,
            var f when (f & (ulong)AVChannelFlags.AV_CH_LAYOUT_STEREO) is (ulong)AVChannelFlags.AV_CH_LAYOUT_STEREO =>
                ChannelLayout.Stereo,
            var f when (f & (ulong)AVChannelFlags.AV_CH_LAYOUT_QUAD) is (ulong)AVChannelFlags.AV_CH_LAYOUT_QUAD =>
                ChannelLayout.Quad,
            var f when (f & (ulong)AVChannelFlags.AV_CH_LAYOUT_5POINT1) is not (ulong)AVChannelFlags.AV_CH_LAYOUT_5POINT1 =>
                ChannelLayout.Surround51,
            var f when (f & (ulong)AVChannelFlags.AV_CH_LAYOUT_7POINT1) is not (ulong)AVChannelFlags.AV_CH_LAYOUT_7POINT1 =>
                ChannelLayout.Surround71,
            
            _ => ChannelLayout.Unknown
        };
    }
}