namespace FFmpegWrapper.Core;

using System.Runtime.InteropServices;
using System.Text;
using CommunityToolkit.HighPerformance.Buffers;

public static class FFHelper
{
    public static string ErrorString(int errno)
    {
        unsafe
        {
            byte* buf = stackalloc byte[AV_ERROR_MAX_STRING_SIZE];
            av_strerror(errno, buf, AV_ERROR_MAX_STRING_SIZE);
            return Marshal.PtrToStringAnsi((nint)buf)!;
        }
    }
    public static int CheckError(this int errno)
    {
        if (errno < 0 && errno is not (int)AVError.AVERROR_EOF) {
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
        if (errno < 0 &&  errno is not (int)AVError.AVERROR_EOF) {
            ThrowError(errno, msg);
        }
        return errno;
    }
    public static void ThrowError(this int errno, string? msg = null)
    {
        msg ??= "Operation failed";
        throw new InvalidOperationException(msg + ": " + ErrorString(errno));
    }

    public static unsafe ReadOnlySpan<byte> Utf8SpanFromPtrNullTerm(byte* handle)
    {
#if NET6_0_OR_GREATER
        return MemoryMarshal.CreateReadOnlySpanFromNullTerminated(handle);
#else
        int len = 0;
        if (handle is null) {
            return ReadOnlySpan<byte>.Empty;
        }
        
        while (handle[len] is not 0) {
            len++;
        }

        return new ReadOnlySpan<byte>(handle, len);
#endif
    }

    public unsafe static ReadOnlySpan<T> GetSpanFromSentinelTerminatedPtr<T>(T* handle, T terminator) where T : unmanaged
    {
        int len = 0;
        if (handle is null) {
            return ReadOnlySpan<T>.Empty;
        }
        
        while (!handle[len].Equals(terminator)) {
            len++;
        }
        
        return new ReadOnlySpan<T>(handle, len);
    }

    public static string SpanToStringUtf8(ReadOnlySpan<byte> span)
    {
        return StringPool.Shared.GetOrAdd(span, Encoding.UTF8);
    }
    
    
#if NET6_0_OR_GREATER
    public static unsafe string PtrToStringUtf8(byte* ptr)
    {
        return SpanToStringUtf8(MemoryMarshal.CreateReadOnlySpanFromNullTerminated(ptr));
    }
#else
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe string PtrToStringUtf8(byte* ptr)
    {
        int length = 0;
        while (ptr[length] is not 0)
            length++;
        
        return SpanToStringUtf8(new ReadOnlySpan<byte>(ptr, length));
    }
#endif

    public static TimeSpan? GetTimeSpan(long pts, Rational timeBase)
    {
        if (pts == AV_NOPTS_VALUE) {
            return null;
        }
        
        return Rational.GetTimeSpan(pts, timeBase);
    }


#if !NETSTANDARD2_1_OR_GREATER
    public static void Deconstruct<TKey, TVal>(this KeyValuePair<TKey, TVal> pair, out TKey key, out TVal val) => (key, val) = (pair.Key, pair.Value);
#endif
    public static long? GetPts(long pts)
    {
        if (pts == AV_NOPTS_VALUE) {
            return null;
        }

        return pts;
    }

    public static void SetPts(ref long pts, long? value)
    {
        if (value is null) {
            pts = AV_NOPTS_VALUE;
            return;
        }
        
        pts = value.Value;
    }
}
