namespace FFmpegWrapper.Core;

public readonly struct FFHandleSource<T> : IEquatable<FFHandleSource<T>>
    where T : unmanaged
{
    public readonly unsafe T** Raw;
    
    public unsafe FFHandleSource(T** handle)
    {
        Raw = handle;
            
        if (Raw is null) {
            throw new ArgumentNullException(nameof(handle));
        }
    }
    
    /// <summary>
    /// Converts <see cref="FFHandle{T}"/> to ref of <see cref="T"/>
    /// </summary>
    /// <param name="handle"></param>
    /// <returns></returns>
    public static unsafe implicit operator T**(FFHandleSource<T> handle)
    {
        if (handle.Raw is null) {
            throw new ArgumentNullException(nameof(handle.Raw));
        }
        
        return handle.Raw;
    }

    /// <summary>
    /// Casts raw pointer to <see cref="FFHandle{T}"/>
    /// </summary>
    /// <param name="handle"></param>
    /// <returns></returns>
    public static unsafe implicit operator FFHandleSource<T>(T** handle)
    {
        return new FFHandleSource<T>(handle);
    }
    
    public bool Equals(FFHandleSource<T> other)
    {
        unsafe
        {
            return Raw == other.Raw;
        }
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is FFHandleSource<T> other && Equals(other);
    }


    /// <inheritdoc />
    public override unsafe int GetHashCode()
    {
        return ((nint)Raw).GetHashCode();
    }
}