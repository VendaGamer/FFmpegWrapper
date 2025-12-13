namespace FFmpegWrapper.Core;

using System.Numerics;

/// <summary>
/// TODO: Comment
/// </summary>
/// <typeparam name="T">Okay</typeparam>
public readonly ref struct FFHandle<T>
    where T : unmanaged
{
    /// <summary>
    /// Unsafe handle to underlying ffmpeg object
    /// </summary>
    public readonly unsafe T* Raw;
    
    /// <summary>
    /// Safe handle to underlying ffmpeg object
    /// </summary>
    public ref T Ref
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            unsafe {
                return ref Unsafe.AsRef<T>(Raw);
            }
        }
    }

    public bool IsNull {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            unsafe {
                return Raw is null;
            }
        }
    }
    
    
    public FFHandle(ref T handle)
    {
        unsafe {
            Raw = (T*) Unsafe.AsPointer(ref handle);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="raw"></param>
    public unsafe FFHandle(T* raw)
    {
        this.Raw = raw;
    }
    
    /// <summary>
    /// Converts <see cref="FFHandle{T}"/> to ref of <see cref="T"/>
    /// </summary>
    /// <param name="handle"></param>
    /// <returns></returns>
    public static unsafe implicit operator T*(FFHandle<T> handle)
    {
        return handle.Raw;
    }

    /// <summary>
    /// Casts raw pointer to <see cref="FFHandle{T}"/>
    /// </summary>
    /// <param name="handle"></param>
    /// <returns></returns>
    public static unsafe implicit operator FFHandle<T>(T* handle)
    {
        return new FFHandle<T>(handle);
    }
    
    public bool Equals(FFHandle<T> other)
    {
        unsafe
        {
            return Raw == other.Raw;
        }
    }

    public static bool operator == (FFHandle<T> a, FFHandle<T> b)
    {
        return a.Equals(b);
    }

    public static bool operator != (FFHandle<T> a, FFHandle<T> b)
    {
        return !a.Equals(b);
    }


    /// <inheritdoc />
    public override int GetHashCode()
    {
        unsafe {
            return ((nint)Raw).GetHashCode();
        }
    }
}
