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
            var res = format.SampleFormat.ToSoundFlow();
            
            if(format.Layout)
            
            return (
                new AudioFormat() {
                    Layout = 
                }
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

    extension(FFmpegWrapper.Media.Formats.ChannelLayout layout)
    {
        public (ChannelLayout channelLayout, bool needsConversion) ToSoundFlow()
            => layout switch {
                
            };
    }
}