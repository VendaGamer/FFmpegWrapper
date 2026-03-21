namespace FFmpegWrapper.Extensions;

using System.Runtime.InteropServices;

using CommunityToolkit.HighPerformance;

public static class SpanExtensions
{
    extension<T>(ReadOnlySpan<T> span) where T : unmanaged
    {
        public unsafe T* RawHandle
        {
            get {
                return (T*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(span));
            }
        }
    }
}