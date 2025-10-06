namespace FFmpegWrapper.Core;

using System.Runtime.InteropServices;

public sealed class CustomBufferPool<TUserData> : BufferPool
    where TUserData : class
{
    public readonly TUserData UserData;
    
    public CustomBufferPool(ulong size, TUserData userData,
        AllocateBuffer? allocFunc = null, FreeUserData? freeUserData = null)
        : base(AllocateBufferPool(size, userData, allocFunc, freeUserData))
    {
        UserData = userData;
    }

    private static FFHandle<AVBufferPool> AllocateBufferPool(
        ulong size, TUserData userData,
        AllocateBuffer? allocFunc = null,
        FreeUserData? freeUserData = null)
    {
        unsafe
        {
            return ffmpeg.av_buffer_pool_init2(size,
                Unsafe.AsPointer(ref userData),
                new av_buffer_pool_init2_alloc_func() {
                    Pointer = Marshal.GetFunctionPointerForDelegate(allocFunc)
                },
                new av_buffer_pool_init2_pool_free_func() {
                    Pointer = Marshal.GetFunctionPointerForDelegate(freeUserData)
                });
        }
    }

    public delegate FFHandle<AVBufferRef> AllocateBufferWithUserData(ulong size, TUserData data);

    public delegate void FreeUserData(TUserData userData);
}