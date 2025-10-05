namespace FFmpegWrapper.Core;

/// <summary>
/// TODO: Comment
/// </summary>
/// <typeparam name="T">Okay</typeparam>
public readonly struct FFHandle<T> : IEquatable<FFHandle<T>>
    where T : unmanaged
{
    /// <summary>
    /// Unsafe handle to underlying ffmpeg object
    /// </summary>
    public readonly unsafe T* Raw;
    
    /// <summary>
    /// Safe handle to underlying ffmpeg object
    /// </summary>
    public readonly ref T Ref
    {
        get
        {
            unsafe {
                return ref Unsafe.AsRef<T>(Raw);
            }
        }
    }
    
    
    public FFHandle(ref T handle)
    {
        unsafe {
            Raw = (T*) Unsafe.AsPointer(ref handle);
            
            if (Raw is null) {
                throw new ArgumentNullException(nameof(handle));
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="raw"></param>
    public unsafe FFHandle(T* raw)
    {
        if (Raw is null) {
            throw new ArgumentNullException(nameof(raw));
        }
        
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
    
    /// <inheritdoc />
    public bool Equals(FFHandle<T> other)
    {
        unsafe
        {
            return Raw == other.Raw;
        }
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is FFHandle<T> other && Equals(other);
    }


    /// <inheritdoc />
    public override int GetHashCode()
    {
        unsafe {
            return Raw->GetHashCode();
        }
    }
}