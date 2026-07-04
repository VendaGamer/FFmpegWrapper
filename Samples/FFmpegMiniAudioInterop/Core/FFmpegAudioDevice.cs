using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BindingWrapperUtils;
using FFmpegBindings.Abstractions;
using FFmpegWrapper.Codecs;
using FFmpegWrapper.Media;
using MiniAudioBindings.Abstractions;
using MiniAudioWrapper;

namespace FFmpegMiniAudioInterop.Core;

public sealed class FFmpegAudioDevice : AudioDevice, IDisposable
{
    public unsafe FFmpegAudioDevice(MediaDemuxer demuxer, NullableHandle<ma_context> context = default)
        : base(CreateConfig(demuxer, out var state), context)
    {
        
    }

    private static unsafe ma_device_config CreateConfig(MediaDemuxer demuxer, out AudioPlaybackState* state)
    {
        state = (AudioPlaybackState*)MiniAudio.ma_calloc((nuint)sizeof(AudioPlaybackState), null);

        if (!demuxer.TryFindBestStream(AVMediaType.AVMEDIA_TYPE_AUDIO, out var stream))
            throw new Exception("Failed to find audio stream.");

        var codec = MediaCodec.GetDecoder(stream.CodecPars.CodecId).Handle.Raw;
        state->CodecContext = FFmpeg.avcodec_alloc_context3(codec);
        FFmpeg.avcodec_parameters_to_context(state->CodecContext, stream.CodecPars.Handle);
        
        if (FFmpeg.avcodec_open2(state->CodecContext, codec, null) < 0)
            throw new Exception("Failed to open codec.");

        state->Packet = FFmpeg.av_packet_alloc();
        state->Frame = FFmpeg.av_frame_alloc();
        state->FormatContext = demuxer; // Assuming demuxer implicitly casts or holds pointer
        state->AudioStreamIndex = stream.Index; 

        int channels = state->CodecContext->ch_layout.nb_channels;
        
        if (state->CodecContext->sample_fmt != AVSampleFormat.AV_SAMPLE_FMT_FLT) {
            SwrContext** swrPtr = &state->SwrContext;
            int res = FFmpeg.swr_alloc_set_opts2(
                swrPtr,
                &state->CodecContext->ch_layout,
                AVSampleFormat.AV_SAMPLE_FMT_FLT,
                state->CodecContext->sample_rate,
                &state->CodecContext->ch_layout,
                state->CodecContext->sample_fmt,
                state->CodecContext->sample_rate,
                0,
                null);

            if (res < 0)
                throw new Exception("Failed to initialize Resampler options.");
            FFmpeg.swr_init(state->SwrContext);
        }
        else
        {
            state->SwrContext = null;
        }

        var cfg = MiniAudio.ma_device_config_init(ma_device_type.ma_device_type_playback);
        cfg.dataCallback = &OnData;
        cfg.pUserData = state;
        cfg.playback.channels = (uint)channels;
        cfg.playback.format = ma_format.ma_format_f32;
        cfg.sampleRate = (uint)state->CodecContext->sample_rate;
        
        return cfg;
    }

    [StructLayout(LayoutKind.Sequential)]
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
        
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static unsafe void OnData(ma_device* pDevice, void* pOutput, void* pInput, uint frameCount)
    {
        var state = (AudioPlaybackState*)pDevice->pUserData;
        if (state == null) return;

        float* outputBuffer = (float*)pOutput;
        int channels = (int)pDevice->playback.channels;
        int totalFramesNeeded = (int)frameCount;
        int totalFramesWritten = 0;

        // 1. Drain remaining samples from a previously decoded frame
        if (state->BufferedFrameCount > 0)
        {
            int framesToCopy = Math.Min(state->BufferedFrameCount, totalFramesNeeded - totalFramesWritten);
            CopyOrResampleFrameData(state, outputBuffer, totalFramesWritten, state->BufferReadOffset, framesToCopy, channels);
            
            totalFramesWritten += framesToCopy;
            state->BufferedFrameCount -= framesToCopy;
            state->BufferReadOffset += framesToCopy;
        }

        // 2. Main decode loop
        while (totalFramesWritten < totalFramesNeeded)
        {
            int response = FFmpeg.avcodec_receive_frame(state->CodecContext, state->Frame);

            if (response == 0)
            {
                state->BufferedFrameCount = state->Frame->nb_samples;
                state->BufferReadOffset = 0;

                int framesToCopy = Math.Min(state->BufferedFrameCount, totalFramesNeeded - totalFramesWritten);
                CopyOrResampleFrameData(state, outputBuffer, totalFramesWritten, state->BufferReadOffset, framesToCopy, channels);

                totalFramesWritten += framesToCopy;
                state->BufferedFrameCount -= framesToCopy;
                state->BufferReadOffset += framesToCopy;
                
                FFmpeg.av_frame_unref(state->Frame);
            }
            else if (response == (int)AVError.AVERROR_EAGAIN)
            {
                bool packetFed = false;
                while (!packetFed)
                {
                    if (FFmpeg.av_read_frame(state->FormatContext, state->Packet) >= 0)
                    {
                        if (state->Packet->stream_index == state->AudioStreamIndex)
                        {
                            int sendResponse = FFmpeg.avcodec_send_packet(state->CodecContext, state->Packet);
                            if (sendResponse == 0)
                            {
                                packetFed = true;
                            }
                        }
                        FFmpeg.av_packet_unref(state->Packet);
                    }
                    else
                    {
                        // End of Stream reached. Fill the rest with absolute silence.
                        int remainingFrames = totalFramesNeeded - totalFramesWritten;
                        NativeMemory.Clear(outputBuffer + (totalFramesWritten * channels), (nuint)(remainingFrames * channels * sizeof(float)));
                        return;
                    }
                }
            }
            else
            {
                int remainingFrames = totalFramesNeeded - totalFramesWritten;
                NativeMemory.Clear(outputBuffer + (totalFramesWritten * channels), (nuint)(remainingFrames * channels * sizeof(float)));
                return;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe void CopyOrResampleFrameData(
        AudioPlaybackState* state, 
        float* outputBuffer, 
        int outputFrameOffset, 
        int inputFrameOffset, 
        int frameCount, 
        int channels)
    {
        float* currentOutputPtr = outputBuffer + (outputFrameOffset * channels);

        if (state->SwrContext != null)
        {
            int bytesPerSample = FFmpeg.av_get_bytes_per_sample(state->CodecContext->sample_fmt);
            bool isPlanar = FFmpeg.av_sample_fmt_is_planar(state->CodecContext->sample_fmt) != 0;

            // Allocate a small array of pointers on the stack for the channel base addresses
            byte** nativeInputPointers = stackalloc byte*[channels];

            for (int i = 0; i < channels; i++)
            {
                if (isPlanar)
                {
                    // Planar: Each channel has its own pointer array. Offset each one independently.
                    nativeInputPointers[i] = state->Frame->extended_data[i] + (inputFrameOffset * bytesPerSample);
                }
                else
                {
                    // Interleaved: Only channel 0 is used. All data is sequential.
                    nativeInputPointers[0] = state->Frame->extended_data[0] + (inputFrameOffset * bytesPerSample * channels);
                    break; 
                }
            }

            FFmpeg.swr_convert(
                state->SwrContext,
                (byte**)&currentOutputPtr,
                frameCount,
                nativeInputPointers,
                frameCount
            );
        }
        else
        {
            // Direct copy if input format is already AV_SAMPLE_FMT_FLT interleaved
            float* inputBuffer = (float*)state->Frame->extended_data[0] + (inputFrameOffset * channels);
            int bytesToCopy = frameCount * channels * sizeof(float);
            Buffer.MemoryCopy(inputBuffer, currentOutputPtr, bytesToCopy, bytesToCopy);
        }
    }
}