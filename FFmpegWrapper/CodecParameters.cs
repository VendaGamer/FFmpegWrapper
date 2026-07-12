namespace FFmpegWrapper;

public readonly ref struct CodecParameters : IHandleObserver<AVCodecParameters>
{
    public unsafe Handle<AVCodecParameters> Handle => (Handle<AVCodecParameters>)_handle;

    public PictureFormat PictureFormat {
        get {
            ref var handle = ref Handle.Ref;
            
            return new PictureFormat(handle.width, handle.height, (AVPixelFormat)handle.format,handle.sample_aspect_ratio);
        }
    }

    public AudioFormat AudioFormat {
        get {
            ref var handle = ref Handle.Ref;
            
            return new AudioFormat((AVSampleFormat)handle.format, handle.sample_rate, handle.ch_layout);
        }
    }
    
    public ref AVCodecID CodecId => ref Handle.Ref.codec_id;
    
    public ref AVMediaType MediaType => ref Handle.Ref.codec_type;

    public ref AVColorTransferCharacteristic ColorCharacteristics => ref Handle.Ref.color_trc;

    private readonly unsafe AVCodecParameters* _handle;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe bool CopyFrom(Handle<AVCodecParameters> source)
        => avcodec_parameters_copy(Handle, source) is 0;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe bool CopyTo(Handle<AVCodecParameters> destination)
        => avcodec_parameters_copy(destination, Handle) is 0;

    public CodecParameters(Handle<AVCodecParameters> handle)
    {
        unsafe {
            _handle = handle;
        }
    }
}
