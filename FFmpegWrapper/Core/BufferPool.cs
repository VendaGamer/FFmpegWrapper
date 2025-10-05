namespace FFmpegWrapper.Core;

using System.Runtime.InteropServices;

public sealed class BufferPool : FFObject<AVBufferPool>
{
    public BufferPool(ulong size, av_buffer_pool_init_alloc? allocFunc = null)
    {
        unsafe {
            _handle = ffmpeg.av_buffer_pool_init(size, allocFunc);
        }
    }
    
    protected override unsafe void Free()
    {
        fixed (AVBufferPool** ptr = &_handle) {
            ffmpeg.av_buffer_pool_uninit(ptr);
        }
    }
}