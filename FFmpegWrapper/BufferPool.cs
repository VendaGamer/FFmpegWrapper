namespace FFmpegWrapper;

using System.Runtime.InteropServices;
using Core;

public abstract class BufferPool : FFObject<AVBufferPool>
{
    protected BufferPool(nuint size)
    {
        unsafe {
            _handle = av_buffer_pool_init(size,
            (delegate* unmanaged[Cdecl]<nuint, AVBufferRef*>)
                Marshal.GetFunctionPointerForDelegate(AllocateBuffer));
        }
    }

    protected BufferPool(Handle<AVBufferPool> handle) : base(handle)
    {

    }
    
    protected abstract Handle<AVBufferRef> AllocateBuffer(nuint size);


    
    protected override unsafe void Free()
    {
        fixed (AVBufferPool** ptr = &_handle) {
            av_buffer_pool_uninit(ptr);
        }
    }
}
