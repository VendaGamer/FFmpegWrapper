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

    public PictureFormat PictureFormat {
        get {
            var handle = Handle.Ref;

            return new PictureFormat(handle.width, handle.height, (AVPixelFormat)handle.format,handle.sample_aspect_ratio);
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
