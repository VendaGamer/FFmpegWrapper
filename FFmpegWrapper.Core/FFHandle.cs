namespace FFmpegWrapper.Core;

/// <summary>
/// TODO: Comment
/// </summary>
/// <typeparam name="T">Okay</typeparam>
public readonly ref struct FFHandle<T>
#if NET9_0_OR_GREATER
    : IEquatable<FFHandle<T>>
#endif
    where T : unmanaged
{
    /// <summary>
    /// Reinterprets the <see langword="T*"/> to <see langword="ref"/> <see cref="T"/>
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

    public unsafe readonly T* Raw;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public FFHandle(ref T handle)
    {
        if(Unsafe.IsNullRef(ref handle))
            throw new ObjectDisposedException(nameof(handle));
        
        unsafe {
            Raw = (T*) Unsafe.AsPointer(ref handle);
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe FFHandle(T* raw)
    {
        if(raw is null)
            throw new ObjectDisposedException(nameof(raw));
        
        Raw = raw;
    }
    
    /// <summary>
    /// Converts <see cref="FFHandle{T}"/> to ref of <see cref="T"/>
    /// </summary>
    /// <param name="handle"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe implicit operator T*(FFHandle<T> handle) => handle.Raw;

    /// <summary>
    /// Casts a raw pointer to <see cref="FFHandle{T}"/>
    /// </summary>
    public static unsafe implicit operator FFHandle<T>(T* handle) => new(handle);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe bool Equals(FFHandle<T> other) => Raw == other.Raw;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator == (FFHandle<T> a, FFHandle<T> b) => a.Equals(b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator != (FFHandle<T> a, FFHandle<T> b) => !a.Equals(b);

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override unsafe int GetHashCode() => ((nint)Raw).GetHashCode();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe void ThrowIfNull()
    {
        if (Raw is null) {
            throw new ObjectDisposedException($"Underlying ffmpeg object {typeof(T).Name} has been disposed.");
        }
    }
}
