namespace FFmpegWrapper;

using Codecs;

using Media;

public readonly struct CodecParameters : IFFHandleObserver<AVCodecParameters>
{
    public unsafe FFHandle<AVCodecParameters> Handle => _handle;

    public PictureFormat PictureFormat {
        get {
            var handle = Handle.Ref;
            
            return new PictureFormat(handle.width, handle.height, (AVPixelFormat)handle.format,handle.sample_aspect_ratio);
        }
    }
    
    public AVCodecID CodecId => Handle.Ref.codec_id;
    
    public AVMediaType MediaType => Handle.Ref.codec_type;

    private readonly unsafe AVCodecParameters* _handle;

    public CodecParameters(FFHandle<AVCodecParameters> handle)
    {
        unsafe
        {
            _handle =  handle;
        }
    }
}
