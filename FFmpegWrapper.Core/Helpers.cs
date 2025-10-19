namespace FFmpegWrapper.Core;

using System.Runtime.InteropServices;
using System.Text;
using CommunityToolkit.HighPerformance.Buffers;
using CommunityToolkit.HighPerformance.Helpers;

public static class FFHelper
{
    public static string ErrorString(int errno)
    {
        unsafe
        {
            byte* buf = stackalloc byte[ffmpeg.AV_ERROR_MAX_STRING_SIZE + 1];
            ffmpeg.av_strerror(errno, buf, ffmpeg.AV_ERROR_MAX_STRING_SIZE);
            return Marshal.PtrToStringAnsi((nint)buf)!;
        }
    }
    public static int CheckError(this int errno)
    {
        if (errno < 0 && errno != ffmpeg.EAGAIN && errno != ffmpeg.AVERROR_EOF) {
            ThrowError(errno);
        }
        return errno;
    }
    /// <summary>
    /// Use when sure that cannot be EAGAIN or EOF
    /// </summary>
    /// <param name="errno">result of ffmpeg call</param>
    /// <returns>true if not error otherwise false</returns>
    public static bool IsSuccess(this int errno)
    {
        return errno >= 0;
    }
    public static int CheckError(this int errno, string msg)
    {
        if (errno < 0 && errno != ffmpeg.EAGAIN && errno != ffmpeg.AVERROR_EOF) {
            ThrowError(errno, msg);
        }
        return errno;
    }
    public static void ThrowError(this int errno, string? msg = null)
    {
        msg ??= "Operation failed";
        
        throw new InvalidOperationException(msg + ": " + ErrorString(errno));
    }

    public static ReadOnlySpan<T> GetSpanFromSentinelTerminatedPtr<T>(FFHandle<T> handle, T terminator) where T : unmanaged
    {
        unsafe
        {
            int len = 0;
            if (handle.Raw is null) {
                return ReadOnlySpan<T>.Empty;
            }
        
            while (!handle.Raw[len].Equals(terminator)) {
                len++;
            }
        
            return new ReadOnlySpan<T>(handle.Raw, len);
        }
    }

    public static string SpanToStringUtf8(ReadOnlySpan<byte> span)
    {
        return StringPool.Shared.GetOrAdd(span, Encoding.UTF8);
    }
    
#if NET6_0_OR_GREATER
    public static string PtrToStringUtf8(FFHandle<byte> ptr)
    {
        unsafe
        {
            return SpanToStringUtf8(MemoryMarshal.CreateReadOnlySpanFromNullTerminated(ptr));
        }
    }
#else
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string PtrToStringUtf8(FFHandle<byte> ptr)
    {
        unsafe
        {
            int length = 0;
            while (ptr.Raw[length] != 0)
                length++;
            
            return SpanToStringUtf8(new ReadOnlySpan<byte>(ptr, length));
        }
    }
#endif

    public static TimeSpan? GetTimeSpan(long pts, Rational timeBase)
    {
        if (pts == ffmpeg.AV_NOPTS_VALUE) {
            return null;
        }
        
        return Rational.GetTimeSpan(pts, timeBase);
    }


#if !NETSTANDARD2_1_OR_GREATER
    public static void Deconstruct<K, V>(this KeyValuePair<K, V> pair, out K key, out V val) => (key, val) = (pair.Key, pair.Value);
#endif
    public static long? GetPts(long pts)
    {
        if (pts == ffmpeg.AV_NOPTS_VALUE) {
            return null;
        }

        return pts;
    }

    public static void SetPts(ref long pts, long? value)
    {
        if (value is null) {
            pts = ffmpeg.AV_NOPTS_VALUE;
            return;
        }
        
        pts = value.Value;
    }
}
