namespace FFmpegWrapper.Extensions;

using System.Runtime.InteropServices;

public static class SpanExtensions
{
    public static ReadOnlySpan<TTarget> Cast<TSource,TTarget>(this ReadOnlySpan<TSource> source)
        where TSource : class, TTarget
        where TTarget : class
    {
        unsafe {
            return new ReadOnlySpan<TTarget>(
                Unsafe.AsPointer(ref MemoryMarshal.GetReference(source)),
                source.Length);
        }
    }
}