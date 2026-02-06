namespace FFmpegWrapper;
using Core;

public readonly struct AVBuffer : IHandleObserver<AVBufferRef>
{
    public Handle<AVBufferRef> Handle {
        get {
            unsafe
            {
                return _handle;
            }
        }
    }

    private readonly unsafe AVBufferRef* _handle;

    public AVBuffer(nuint size)
    {
        unsafe
        {
            _handle = av_buffer_alloc(size);
        }
    }
}
