namespace FFmpegWrapper;

using System.Runtime.InteropServices;

using Core;

public abstract class CustomBufferPool<TUserData>
    where TUserData : unmanaged
{
    public readonly TUserData UserData;
    private av_buffer_pool_init2_alloc _alloc;
    private av_buffer_pool_init2_pool_free _free;
    
    public CustomBufferPool(nuint size, TUserData userData,
        AllocateBufferWithUserData? allocFunc = null, FreeUserData? freeUserData = null)
    {
        UserData = userData;
        
        unsafe
        {
            _alloc = NativeAlloc;
            _free = NativeFree;
        

            av_buffer_pool_init2(size, Unsafe.AsPointer(ref userData), 
                (delegate* unmanaged[Cdecl]<void*, nuint, AVBufferRef*>)
                        Marshal.GetFunctionPointerForDelegate(_alloc),
                    (delegate* unmanaged[Cdecl]<void*, void>)
                        Marshal.GetFunctionPointerForDelegate(_free));


            AVBufferRef* NativeAlloc(void* opaque, nuint size)
            {
                return allocFunc((TUserData*)opaque, size).Raw;
            }

            void NativeFree(void* opaque)
            {
                freeUserData((TUserData*)opaque);
            }
        }
    }
    
    public delegate FFHandle<AVBufferRef> AllocateBufferWithUserData(FFHandle<TUserData> data, nuint size);
    public delegate void FreeUserData(FFHandle<TUserData> userData);
}
