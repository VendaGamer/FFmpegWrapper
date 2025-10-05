namespace FFmpegWrapper.Extensions;

public static class IHandleExtensions
{
    /// <summary>
    /// Returns whenever <see cref="IHandle{T}.Handle"/> is null
    /// </summary>
    /// <remarks>
    /// Useful when not wanting to deal with unsafe code
    /// </remarks>
    /// <returns>true if <see cref="IHandle{T}.Handle"/> is null otherwise false</returns>
    public static bool IsNull<T>(this IHandle<T> ptr) where T : unmanaged
    {
        unsafe
        {
            return ptr.Handle is null;
        }
    }
    
    public static ref T AsRef<T>(this IHandle<T> ptr) where T : unmanaged
    {
        unsafe
        {
            return ref Unsafe.AsRef<T>(ptr.Handle);
        }
    }
}