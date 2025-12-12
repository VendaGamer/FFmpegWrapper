namespace FFmpegWrapper.Extensions;

using System.Runtime.InteropServices;

using CommunityToolkit.HighPerformance;

public static class SpanExtensions
{

    extension<T>(ReadOnlySpan<T> span)
    where T: unmanaged
    {
        public unsafe T* RawHandle => !span.IsEmpty ? (T*)Unsafe.AsPointer(ref span.DangerousGetReference()) : null;

    }

    extension(ReadOnlySpan<byte> span)
    {
        public string ToStringUft8() => FFHelper.SpanToStringUtf8(span);
    }
    
    extension<TSource, TTarget>(ReadOnlySpan<TSource> source) where TSource : class, TTarget where TTarget : class
    {
        public ReadOnlySpan<TTarget> Cast()
        {
            unsafe {
                return new ReadOnlySpan<TTarget>(
                    Unsafe.AsPointer(ref MemoryMarshal.GetReference(source)),
                    source.Length);
            }
        }
    }
}
