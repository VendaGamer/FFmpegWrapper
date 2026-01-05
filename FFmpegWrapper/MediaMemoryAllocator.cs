namespace FFmpegWrapper;

public static class MediaMemoryAllocator
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Span<T> Allocate<T>(nuint size)
        where T : unmanaged
    {
        unsafe {
            var buffer = av_malloc(size);
            
            if(buffer is null)
                throw new Exception("Could not allocate memory for buffer.");
            
            return new Span<T>(buffer, (int)size);
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Span<T> AllocateZero<T>(nuint size)
        where T : unmanaged
    {
        unsafe {
            var buffer = av_mallocz(size);
            
            if(buffer is null)
                throw new Exception("Could not allocate memory for buffer.");
            
            return new Span<T>(buffer, (int)size);
        }
    }
}