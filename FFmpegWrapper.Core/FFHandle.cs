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
    public unsafe T* Raw {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if (_handle is null) {
                throw new ObjectDisposedException($"Underlying ffmpeg object {typeof(T).Name} has been disposed.");
            }
            return _handle;
        }
    }
    
    /// <summary>
    /// Safe handle to underlying ffmpeg object
    /// </summary>
    public ref T Ref
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            unsafe {
                if (_handle is null) {
                    throw new ObjectDisposedException($"Underlying ffmpeg object {typeof(T).Name} has been disposed.");
                }
                
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

    private unsafe readonly T* _handle;
    
    public FFHandle(ref T handle)
    {
        unsafe {
            _handle = (T*) Unsafe.AsPointer(ref handle);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="raw"></param>
    public unsafe FFHandle(T* raw)
    {
        _handle = raw;
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
            return _handle == other._handle;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator == (FFHandle<T> a, FFHandle<T> b) => a.Equals(b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator != (FFHandle<T> a, FFHandle<T> b) => !a.Equals(b);


    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override unsafe int GetHashCode() => ((nint)_handle).GetHashCode();
}
