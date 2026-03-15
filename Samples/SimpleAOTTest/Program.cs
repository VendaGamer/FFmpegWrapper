using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using MiniAudioBindings.Abstractions;
using MiniAudioBindings.Linked;
using MiniAudioWrapper;
using MiniAudioWrapper.Codecs.Decoding;
using MiniAudioWrapper.Configuration;

class Program
{

    private static AudioDecoder decoder;
    private static AudioContext context;

    private static readonly ma_backend[] Backends = [ma_backend.ma_backend_pulseaudio];
    
    static unsafe void Main(string[] args)
    {
        MiniAudioLinked.Init();
        
        context = new AudioContext(new AudioContextConfig(), Backends);
        decoder = new AudioDecoder("test.mp3"u8, new AudioDecoderConfig());

        ref var dec = ref decoder.Handle.Ref;

        var devCfg = new AudioDeviceConfig(ma_device_type.ma_device_type_playback);
        ref var config = ref devCfg.Handle.Ref;
        
        config.playback.format    = dec.outputFormat;
        config.playback.channels  = dec.outputChannels;
        config.sampleRate         = dec.outputSampleRate;
        config.dataCallback       = &data_callback;
        
        var device = new AudioDevice(devCfg, context.Handle);
        device.Start();

        Console.ReadLine();
    }
    
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    static unsafe void data_callback(ma_device* device, void* pOutput, void* pInput, uint frameCount)
    {
        MiniAudio.ma_decoder_read_pcm_frames(decoder.Handle, pOutput, frameCount, null);
    }
}


