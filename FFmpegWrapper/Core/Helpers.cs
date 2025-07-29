namespace FFmpegWrapper.Core;

using System.Runtime.InteropServices;
using System.Text;
using CommunityToolkit.HighPerformance.Buffers;

internal static unsafe class Helpers
{
    public static string ErrorString(int errno)
    {
        byte* buf = stackalloc byte[ffmpeg.AV_ERROR_MAX_STRING_SIZE + 1];
        ffmpeg.av_strerror(errno, buf, ffmpeg.AV_ERROR_MAX_STRING_SIZE);
        return Marshal.PtrToStringAnsi((nint)buf)!;
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
    public static Exception ThrowError(this int errno, string? msg = null)
    {
        msg ??= "Operation failed";
        
        throw new InvalidOperationException(msg + ": " + ErrorString(errno));
    }

    public static ReadOnlySpan<T> GetSpanFromSentinelTerminatedPtr<T>(T* ptr, T terminator) where T : unmanaged
    {
        int len = 0;
        if (ptr == null) {
            return ReadOnlySpan<T>.Empty;
        }
        
        while (!ptr[len].Equals(terminator)) {
            len++;
        }
        return new ReadOnlySpan<T>(ptr, len);
    }

    public static string SpanToStringUTF8(ReadOnlySpan<byte> span)
    {
        return StringPool.Shared.GetOrAdd(span, Encoding.UTF8);
    }
    
#if NET6_0_OR_GREATER
    public static string PtrToStringUTF8(byte* ptr)
    {
        return SpanToStringUTF8(MemoryMarshal.CreateReadOnlySpanFromNullTerminated(ptr));
    }
#else
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string PtrToStringUTF8(byte* ptr)
    {
        int length = 0;
        while (ptr[length] != 0)
            length++;
        
        return SpanToStringUTF8(new ReadOnlySpan<byte>(ptr, length));
    }
#endif
    public static bool StrCmp(byte* a, ReadOnlySpan<byte> b)
    {
        for (int i = 0; i < b.Length; i++) {
            if (a[i] == 0 || a[i] != b[i]) {
                return false;
            }
        }
        return true;
    }

    public static long? GetPTS(long pts) => pts != ffmpeg.AV_NOPTS_VALUE ? pts : null;
    public static void SetPTS(ref long pts, long? value) => pts = value ?? ffmpeg.AV_NOPTS_VALUE;

    public static TimeSpan? GetTimeSpan(long pts, Rational timeBase)
    {
        if (pts == ffmpeg.AV_NOPTS_VALUE) {
            return null;
        }
        
        return Rational.GetTimeSpan(pts, timeBase);
    }
    
    public static IntPtr StringToHGlobalUTF8(string? s, out int length)
    {
        if (s is null)
        {
            length = 0;
            return IntPtr.Zero;
        }

        var bytes = Encoding.UTF8.GetBytes(s);
        var ptr = Marshal.AllocHGlobal(bytes.Length + 1);
        Marshal.Copy(bytes, 0, ptr, bytes.Length);
        Marshal.WriteByte(ptr, bytes.Length, 0);
        length = bytes.Length;

        return ptr;
    }


#if !NETSTANDARD2_1_OR_GREATER
    public static void Deconstruct<K, V>(this KeyValuePair<K, V> pair, out K key, out V val) => (key, val) = (pair.Key, pair.Value);
#endif
}
