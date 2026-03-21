namespace FFmpegWrapper;

public readonly ref struct CodecParameters : IHandleObserver<AVCodecParameters>
{
    public unsafe Handle<AVCodecParameters> Handle => (Handle<AVCodecParameters>)_handle;

    public PictureFormat PictureFormat {
        get {
            var handle = Handle.Ref;
            
            return new PictureFormat(handle.width, handle.height, (AVPixelFormat)handle.format,handle.sample_aspect_ratio);
        }
    }

    public AudioFormat AudioFormat {
        get {
            var handle = Handle.Ref;
            
            return new AudioFormat((AVSampleFormat)handle.format, handle.sample_rate, handle.ch_layout);
        }
    }
    
    public AVCodecID CodecId => Handle.Ref.codec_id;
    
    public AVMediaType MediaType => Handle.Ref.codec_type;

    public AVColorTransferCharacteristic ColorCharacteristics => Handle.Ref.color_trc;

    private readonly unsafe AVCodecParameters* _handle;

    public CodecParameters(Handle<AVCodecParameters> handle)
    {
        unsafe {
            _handle =  handle;
        }
    }
}
