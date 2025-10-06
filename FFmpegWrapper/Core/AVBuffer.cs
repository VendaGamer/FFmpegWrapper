namespace FFmpegWrapper.Core;

public readonly struct AVBuffer : IFFHandle<AVBufferRef>
{
    public FFHandle<AVBufferRef> Handle {
        get {
            unsafe
            {
                return _handle;
            }
        }
    }

    private readonly unsafe AVBufferRef* _handle;

    public AVBuffer(ulong size)
    {
        unsafe
        {
            _handle = ffmpeg.av_buffer_alloc(size);
        }
    }
}