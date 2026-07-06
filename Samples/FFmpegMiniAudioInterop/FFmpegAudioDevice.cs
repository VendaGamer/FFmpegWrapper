namespace FFmpegMiniAudioInterop;

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using BindingWrapperUtils;

using FFmpegBindings.Abstractions;

using FFmpegWrapper.Extensions;
using FFmpegWrapper.Media;

using MiniAudioBindings.Abstractions;

using MiniAudioWrapper;

public sealed class FFmpegAudioDevice : AudioDevice
{
    public FFmpegAudioDevice(MediaDemuxer demuxer, NullableHandle<ma_context> context = default)
        : base(Configure(demuxer), context)
    {
        
    }

    private static ma_device_config Configure(MediaDemuxer demuxer)
    {
        if(!demuxer.TryFindBestStream(AVMediaType.AVMEDIA_TYPE_AUDIO, out var stream).IsSuccess)
        
        
        unsafe {
            Native = MiniAudio.ma_device_config_init(ma_device_type.ma_device_type_playback);
            Native.dataCallback = &OnData;
            Native.pUserData = ctx;
            Native.playback.channels = (uint)ctx.Ref.ch_layout.nb_channels;
            Native.playback.format = ToMaFormat(ctx.Ref.sample_fmt);
            Native.sampleRate = (uint)ctx.Ref.sample_rate;
        }
    }

    protected override unsafe void Free()
    {
        
    }
    
    
    private static ma_format ToMaFormat(AVSampleFormat sampleFormat)
            => sampleFormat switch {
                AVSampleFormat.AV_SAMPLE_FMT_U8 or AVSampleFormat.AV_SAMPLE_FMT_U8P => ma_format.ma_format_u8,
                AVSampleFormat.AV_SAMPLE_FMT_S16 or AVSampleFormat.AV_SAMPLE_FMT_S16P => ma_format.ma_format_s16,
                AVSampleFormat.AV_SAMPLE_FMT_S32 or AVSampleFormat.AV_SAMPLE_FMT_S32P => ma_format.ma_format_s32,
                AVSampleFormat.AV_SAMPLE_FMT_FLT or AVSampleFormat.AV_SAMPLE_FMT_FLTP => ma_format.ma_format_f32,
                _ => ma_format.ma_format_unknown
            };
    
    
    
    
    private unsafe struct AudioPlaybackState
    {
        public AVFormatContext* FormatContext;
        public AVCodecContext* CodecContext;
        public SwrContext* SwrContext;
        public AVPacket* Packet;
        public AVFrame* Frame;
        public int AudioStreamIndex;
        public int BufferedFrameCount;
        public int BufferReadOffset;
    }
}