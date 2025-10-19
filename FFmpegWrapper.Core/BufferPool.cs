namespace FFmpegWrapper.Core;

using System.Buffers;
using System.Runtime.InteropServices;

public class BufferPool : FFObject<AVBufferPool>
{
    public BufferPool(ulong size, AllocateBuffer? allocFunc = null)
    {
        unsafe {
            _handle = ffmpeg.av_buffer_pool_init(size, new av_buffer_pool_init_alloc_func{
                Pointer = Marshal.GetFunctionPointerForDelegate(allocFunc)
            });
        }
    }

    protected BufferPool(FFHandle<AVBufferPool> handle)
    {
        unsafe
        {
            _handle = handle;
        }
    }
    
    protected override unsafe void Free()
    {
        fixed (AVBufferPool** ptr = &_handle) {
            ffmpeg.av_buffer_pool_uninit(ptr);
        }
    }
    
    public delegate FFHandle<AVBufferRef> AllocateBuffer(ulong size);
}