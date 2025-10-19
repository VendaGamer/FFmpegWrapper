namespace FFmpegWrapper;

using Media;

public readonly struct CodecParameters : IFFHandleObserver<AVCodecParameters>
{
    public FFHandle<AVCodecParameters> Handle {
        get {
            unsafe {
                return _handle;
            }
        }
    }
    
    public MediaType MediaType => (MediaType)Handle.Ref.codec_type;

    private readonly unsafe AVCodecParameters* _handle;

    public CodecParameters(FFHandle<AVCodecParameters> handle)
    {
        unsafe
        {
            _handle =  handle;
        }
    }
}