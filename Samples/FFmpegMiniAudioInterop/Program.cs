using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using BindingWrapperUtils;

using FFmpegBindings.Abstractions;
using FFmpegBindings.Linked;

using FFmpegWrapper.Codecs.Decoding;
using FFmpegWrapper.Containers;
using FFmpegWrapper.Media;
using FFmpegWrapper.Media.Formats;

using MiniAudioBindings.Abstractions;
using MiniAudioWrapper;

FFmpegLinked.Init();
var device = new AudioDevice(new ma_device_config {
    
});


public readonly ref struct DemuxerPlaybackConfig
{
    private readonly ma_device_config Native;
    
    public DemuxerPlaybackConfig(Handle<AVCodecContext> ctx)
    {
        Debug.Assert(ctx.Ref.);
        unsafe {
            Native = MiniAudio.ma_device_config_init(ma_device_type.ma_device_type_playback);
            Native.dataCallback = &OnData;
            Native.pUserData = ctx;
            Native.playback.channels = (uint)ctx.Ref.ch_layout.nb_channels;
            Native.playback.format = ToMaFormat(ctx.Ref.sample_fmt);
            Native.sampleRate = (uint)ctx.Ref.sample_rate;
        }
    }

    private static ma_format ToMaFormat(AVSampleFormat sampleFormat)
        => sampleFormat switch {
            AVSampleFormat.AV_SAMPLE_FMT_U8 or AVSampleFormat.AV_SAMPLE_FMT_U8P => ma_format.ma_format_u8,
            AVSampleFormat.AV_SAMPLE_FMT_S16 or AVSampleFormat.AV_SAMPLE_FMT_S16P => ma_format.ma_format_s16,
            AVSampleFormat.AV_SAMPLE_FMT_S32 or AVSampleFormat.AV_SAMPLE_FMT_S32P => ma_format.ma_format_s32,
            AVSampleFormat.AV_SAMPLE_FMT_FLT or AVSampleFormat.AV_SAMPLE_FMT_FLTP => ma_format.ma_format_f32,
            _ => ma_format.ma_format_unknown
        };
    
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static unsafe void OnData(ma_device* pDevice, void* pOutput, void* pInput, uint frameCount)
    {
        var ctx = (AVFormatContext*)pDevice->pUserData;
        
        if (ctx is null)
            return;
        
        byte* output = (byte*)pOutput;

        ctx->
        int bytesPerFrame = (int)pDevice->playback.channels * sizeof(float);
        int framesWritten = 0;

        while (framesWritten < frameCount)
        {
            if (state->DecodedSamplesRemaining == 0)
            {
                AVPacket* packet = ffmpeg.av_packet_alloc();

                if (ffmpeg.av_read_frame(state->FormatCtx, packet) < 0)
                {
                    // EOF -> silence
                    Unsafe.InitBlock(
                        output + framesWritten * bytesPerFrame,
                        0,
                        (uint)((frameCount - framesWritten) * bytesPerFrame));

                    ffmpeg.av_packet_free(&packet);
                    return;
                }

                if (packet->stream_index == state->AudioStreamIndex)
                {
                    ffmpeg.avcodec_send_packet(state->CodecCtx, packet);

                    int ret = ffmpeg.avcodec_receive_frame(state->CodecCtx, state->Frame);

                    if (ret >= 0)
                    {
                        // Convert to device format (f32/interleaved)
                        int converted = ffmpeg.swr_convert(
                            state->SwrCtx,
                            &state->ConvertedBuffer,
                            state->Frame->nb_samples,
                            (byte**)state->Frame->extended_data,
                            state->Frame->nb_samples);

                        state->DecodedBuffer = state->ConvertedBuffer;
                        state->DecodedSamplesRemaining = converted;
                        state->DecodedBufferOffset = 0;
                    }
                }

                ffmpeg.av_packet_free(&packet);
            }

            int framesToCopy = Math.Min(
                (int)(frameCount - framesWritten),
                state->DecodedSamplesRemaining);

            int bytesToCopy = framesToCopy * bytesPerFrame;

            Buffer.MemoryCopy(
                state->DecodedBuffer + state->DecodedBufferOffset,
                output + framesWritten * bytesPerFrame,
                bytesToCopy,
                bytesToCopy);

            state->DecodedBufferOffset += bytesToCopy;
            state->DecodedSamplesRemaining -= framesToCopy;
            framesWritten += framesToCopy;
        }
    }
}