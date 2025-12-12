namespace FFmpegWrapper;

using System.Runtime.InteropServices;

using Core;

public class BufferPool : FFObject<AVBufferPool>
{
    private av_buffer_pool_init_alloc _alloc;
    public BufferPool(nuint size, AllocateBuffer? allocFunc = null)
    {
        unsafe {
            
            _alloc = NativeAlloc;
            _handle = av_buffer_pool_init(size, (delegate* unmanaged[Cdecl]<nuint, AVBufferRef*>)
                Marshal.GetFunctionPointerForDelegate(_alloc));
            
            AVBufferRef* NativeAlloc(nuint size)
            {
                return allocFunc(size);
            }
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
            av_buffer_pool_uninit(ptr);
        }
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public unsafe delegate AVBufferRef* av_buffer_pool_init_alloc(nuint size);
    public delegate FFHandle<AVBufferRef> AllocateBuffer(nuint size);
}
