namespace FFmpegWrapper.Core;

using System.Numerics;
using System.Runtime.InteropServices;

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
        return new ReadOnlySpan<byte>(handle, Strlen(handle));
#endif
    }

    public unsafe static ReadOnlySpan<T> GetSpanFromSentinelTerminatedPtr<T>(T* handle, T terminator) where T : unmanaged
    {
        if (handle is null) {
            return ReadOnlySpan<T>.Empty;
        }
        
        int len = 0;
        while (!handle[len].Equals(terminator)) {
            len++;
        }
        
        return new ReadOnlySpan<T>(handle, len);
    }
    
#if !NETCOREAPP3_0_OR_GREATER
    private static ReadOnlySpan<int> DeBruijnTable => [
        0,  1,  2, 53,  3,  7, 54, 27,
        4, 38, 41,  8, 34, 55, 48, 28,
        62,  5, 39, 46, 44, 42, 22,  9,
        35, 56, 49, 36, 29, 63, 21, 23,
        14, 31, 13, 30, 16, 20, 19, 26,
        10, 37, 40, 47, 43, 21, 24, 15,
        17, 32, 18, 25, 11, 12, 33, 45,
        50, 51, 52,  6, 60, 61, 59, 58
    ];
#endif
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int TrailingZeroCount(ulong v)
    {
#if NETCOREAPP3_0_OR_GREATER
        return BitOperations.TrailingZeroCount(v);
#else
        if (v is 0)
            return 64;
        
        return DeBruijnTable[(int)(((ulong)((long)v & -(long)v) * 0x022FDD63CC95386DUL) >> 58)];
#endif
    }

    public static unsafe int Strlen(byte* str)
    {
        ulong* wordPtr = (ulong*)((nuint)str & ~7UL);
        
        int offset = (int)((nuint)str & 7);
        ulong word = *wordPtr;
        
        ulong mask = ulong.MaxValue << (offset * 8);
        word |= ~mask;

        if (HasZeroByte(word))
            return IndexOfFirstZeroByte((word & mask) is 0 ? word : (word)) - offset;
        
        do { word = *++wordPtr; }
        while (!HasZeroByte(word));

        return (int)((byte*)wordPtr - str) + IndexOfFirstZeroByte(word);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool HasZeroByte(ulong v)
            => ((v - 0x0101010101010101UL) & (~v & 0x8080808080808080UL)) is not 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int IndexOfFirstZeroByte(ulong v)
        {
            ulong mask = (v - 0x0101010101010101UL) & ~v & 0x8080808080808080UL;
        
            return TrailingZeroCount(mask) >> 3;
        }
    }
    
    
#if NET6_0_OR_GREATER
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe string PtrToStringUtf8(byte* ptr)
    {
        return MemoryMarshal.CreateReadOnlySpanFromNullTerminated(ptr).ToStringUft8();
    }
#else
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe string PtrToStringUtf8(byte* ptr)
    {
        int length = 0;
        while (ptr[length] is not 0)
            length++;
        
        return new ReadOnlySpan<byte>(ptr, length).ToStringUft8();
    }
#endif


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
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Handle Allocate(nuint size)
    {
        unsafe {
            var buffer = av_malloc(size);
            
            if(buffer is null)
                throw new OutOfMemoryException("Could not allocate memory for buffer.");

            return WrapperHelper.UnsafeGeneralHandle(buffer);
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Handle AllocateZeroed(nuint size)
    {
        unsafe {
            var buffer = av_mallocz(size);
            
            if(buffer is null)
                throw new OutOfMemoryException("Could not allocate memory for buffer.");

            return WrapperHelper.UnsafeGeneralHandle(buffer);
        }
    }
}
