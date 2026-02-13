using System.Buffers;
using System.Runtime.CompilerServices;

using static FFmpegBindings.Abstractions.FFmpeg;
using static MiniAudioBindings.Abstractions.MiniAudio;

using System.Runtime.InteropServices;
using System.Text;
using BindingWrapperUtils.Extensions;
using FFmpegBindings.Abstractions;
using FFmpegBindings.Linked;
using MiniAudioBindings.Abstractions;
using MiniAudioBindings.Linked;


public unsafe class FFmpegMiniaudioPlayer
{
    private ma_device device;
    private ma_decoder decoder;
    private byte[] audioBuffer;
    private int bufferSize;
    private int readPosition;
    private readonly Lock _lockObject = new();

    public void PlayFile(ReadOnlySpan<byte> filePath)
    {
        AVFormatContext* formatContext = null;
        AVCodecContext* codecContext = null;
        AVFrame* frame = null;
        AVPacket* packet = null;
        SwrContext* swrContext = null;

        try
        {
            formatContext = avformat_alloc_context();
            int result = avformat_open_input(&formatContext, filePath.RawHandle, null, null);
            if (result < 0)
                throw new Exception($"Could not open file: {filePath.ToStringUft8()}");
            
            if (avformat_find_stream_info(formatContext, null) < 0)
                throw new Exception("Could not find stream information");
            
            int audioStreamIndex = -1;
            for (int i = 0; i < formatContext->nb_streams; i++)
            {
                if (formatContext->streams[i]->codecpar->codec_type == AVMediaType.AVMEDIA_TYPE_AUDIO)
                {
                    audioStreamIndex = i;
                    break;
                }
            }

            if (audioStreamIndex == -1)
                throw new Exception("Could not find audio stream");

            AVStream* audioStream = formatContext->streams[audioStreamIndex];
            AVCodecParameters* codecParams = audioStream->codecpar;
            
            AVCodec* codec = avcodec_find_decoder(codecParams->codec_id);
            if (codec == null)
                throw new Exception("Unsupported codec");
            
            codecContext = avcodec_alloc_context3(codec);
            avcodec_parameters_to_context(codecContext, codecParams);
            
            if (avcodec_open2(codecContext, codec, null) < 0)
                throw new Exception("Could not open codec");
            
            Console.WriteLine($"Audio Format: {codecContext->sample_fmt}");
            Console.WriteLine($"Sample Rate: {codecContext->sample_rate}");
            Console.WriteLine($"Channels: {codecContext->ch_layout.nb_channels}");
            
            int targetSampleRate = 48000;
            int targetChannels = 2;
            AVSampleFormat targetFormat = AVSampleFormat.AV_SAMPLE_FMT_FLT;
            AVChannelLayout outLayout;
            AVChannelLayout inLayout;

            av_channel_layout_default(&inLayout, codecContext->ch_layout.nb_channels);
            av_channel_layout_default(&outLayout, targetChannels);

            {
                var res = swr_alloc_set_opts2(
                    &swrContext,
                    &outLayout,
                    targetFormat,
                    targetSampleRate,
                    &inLayout,
                    codecContext->sample_fmt,
                    codecContext->sample_rate,
                    0,
                    null
                );

                swr_init(swrContext);
                
                if (res < 0)
                    throw new Exception("Could not initialize resampler"); 
            }
            
            frame = av_frame_alloc();
            packet = av_packet_alloc();
            
            var decodedData = new List<byte>();

            while (av_read_frame(formatContext, packet) >= 0)
            {
                if (packet->stream_index == audioStreamIndex)
                {
                    result = avcodec_send_packet(codecContext, packet);
                    if (result < 0)
                    {
                        Console.WriteLine("Error sending packet for decoding");
                        continue;
                    }
                    
                    while (result >= 0)
                    {
                        result = avcodec_receive_frame(codecContext, frame);
                        
                        if ((AVError)result is AVError.AVERROR_EAGAIN or AVError.AVERROR_EOF)
                            break;
                        
                        if (result < 0)
                            throw new Exception("Error during decoding");
                        
                        int outSamples = (int)av_rescale_rnd(
                            swr_get_delay(swrContext, codecContext->sample_rate) + frame->nb_samples,
                            targetSampleRate,
                            codecContext->sample_rate,
                            AVRounding.AV_ROUND_UP
                        );

                        byte** convertedData = null;
                        av_samples_alloc_array_and_samples(
                            &convertedData,
                            null,
                            targetChannels,
                            outSamples,
                            targetFormat,
                            0
                        );

                        int convertedSamples = swr_convert(
                            swrContext,
                            convertedData,
                            outSamples,
                            &frame->data._0,
                            frame->nb_samples
                        );

                        if (convertedSamples > 0)
                        {
                            int bufferSize = av_samples_get_buffer_size(
                                null,
                                targetChannels,
                                convertedSamples,
                                targetFormat,
                                1
                            );

                            if (bufferSize < 0)
                                throw new Exception("Could not get buffer size for samples");

                            byte[] managedBuffer = new byte[bufferSize];
                            Marshal.Copy((IntPtr)convertedData![0], managedBuffer, 0, bufferSize);
                            decodedData.AddRange(managedBuffer);
                        }

                        if (convertedData != null)
                        {
                            av_freep(&convertedData[0]);
                            av_freep(&convertedData);
                        }
                    }
                }

                av_packet_unref(packet);
            }
            
            avcodec_send_packet(codecContext, null);
            while (avcodec_receive_frame(codecContext, frame) >= 0)
            {
                
            }
            
            audioBuffer = decodedData.ToArray();
            bufferSize = audioBuffer.Length;
            readPosition = 0;

            Console.WriteLine($"Decoded {bufferSize} bytes of audio data");
            PlayWithMiniaudio(targetSampleRate, targetChannels);
        }
        finally
        {
            if (packet is not null)
            {
                av_packet_free(&packet);
            }
            if (frame is not null)
            {
                av_frame_free(&frame);
            }
            if (swrContext is not null)
                swr_free(&swrContext);
            if (codecContext is not null)
            {
                avcodec_free_context(&codecContext);
            }
            if (formatContext != null)
            {
                avformat_close_input(&formatContext);
            }
        }
    }

    private void PlayWithMiniaudio(int sampleRate, int channels)
    {
        ma_device_config config = ma_device_config_init(ma_device_type.ma_device_type_playback);
        config.playback.format = ma_format.ma_format_f32;
        config.playback.channels = (uint)channels;
        config.sampleRate = (uint)sampleRate;
        config.dataCallback = &DataCallback;
        config.pUserData = (void*)GCHandle.ToIntPtr(GCHandle.Alloc(this));
        
        fixed (ma_device* ptr = &device)
        {
            if (ma_device_init(null, &config, ptr) is not ma_result.MA_SUCCESS)
                throw new Exception("Failed to initialize miniaudio device");
            
            if (ma_device_start(ptr) is not ma_result.MA_SUCCESS)
            {
                ma_device_uninit(ptr);
                throw new Exception("Failed to start miniaudio device");
            }

            Console.WriteLine("Playing audio... Press any key to stop.");
            Console.ReadKey();

            ma_device_uninit(ptr);
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void DataCallback(ma_device* pDevice, void* pOutput, void* pInput, uint frameCount)
    {
        GCHandle handle = GCHandle.FromIntPtr((IntPtr)pDevice->pUserData);
        FFmpegMiniaudioPlayer player = (FFmpegMiniaudioPlayer)handle.Target!;

        lock (player._lockObject)
        {
            fixed (ma_decoder* ptr = &player.decoder) {
                
            }
            
            int bytesToRead = (int)(frameCount * sizeof(float) * 2);
            int bytesAvailable = player.bufferSize - player.readPosition;
            int bytesToCopy = Math.Min(bytesToRead, bytesAvailable);

            if (bytesToCopy > 0) {
                
                Marshal.Copy(
                    player.audioBuffer,
                    player.readPosition,
                    (IntPtr)pOutput,
                    bytesToCopy
                );
                player.readPosition += bytesToCopy;
            }
        }
    }
}

class Program
{
    static void Main(string[] args)
    {
        FFmpegLinked.Init();
        MiniAudioLinked.Init();
        
        var player = new FFmpegMiniaudioPlayer();
        player.PlayFile(args.Length is not 0 ? Encoding.UTF8.GetBytes(args[0]) : "test.wav"u8);
    }
}