namespace FFmpegWrapper.Core;

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

/// <summary>
/// <see cref="FFHandle{T}"/> alternative that allows to use <see langword="null"/> values.
/// </summary>
/// <typeparam name="T"></typeparam>
public readonly ref struct NullableFFHandle<T>
#if NET9_0_OR_GREATER
    : IEquatable<NullableFFHandle<T>>, IEquatable<FFHandle<T>>
#endif
    where T : unmanaged
{

    /// <summary>
    /// Gets a <see cref="NullableFFHandle{T}"/> instance representing a <see langword="null"/> reference.
    /// </summary>
    public static NullableFFHandle<T> Null
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => default;
    }
    
    /// <summary>
    /// Gets a value indicating whether or not the current <see cref="NullableFFHandle{T}"/>
    /// instance wraps a valid reference that can be accessed.
    /// </summary>
    public bool IsNull {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            unsafe
            {
                return _handle is null;
            }
        }
    }
    
    /// <summary>
    /// Gets the underlying <see cref="FFHandle{T}"/> instance if it is not null.
    /// </summary>
    /// <remarks>
    /// If a null handle is acceptable, use <see cref="op_Implicit(FFmpegWrapper.Core.NullableFFHandle{T})"/>
    /// </remarks>
    /// <exception cref="InvalidOperationException">Thrown when trying to access a null handle</exception> instead
    public FFHandle<T> Handle {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            unsafe
            {
                if (IsNull)
                    throw new InvalidOperationException("Trying to access a null handle");
            
                return _handle;
            }
        }
    }

    private readonly unsafe T* _handle;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe NullableFFHandle(T* handle) => _handle = handle;
    

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(NullableFFHandle<T> other)
    {
        unsafe {
            return _handle == other._handle;
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(FFHandle<T> other)
    {
        unsafe {
            return _handle == other.Raw;
        }
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override unsafe int GetHashCode() => ((nint)_handle).GetHashCode();
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator == (NullableFFHandle<T> a, NullableFFHandle<T> b) => a.Equals(b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator != (NullableFFHandle<T> a, NullableFFHandle<T> b) => !a.Equals(b);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator == (NullableFFHandle<T> a, FFHandle<T> b) => a.Equals(b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator != (NullableFFHandle<T> a, FFHandle<T> b) => !a.Equals(b);
    
    
    /// <summary>
    /// Casts a <see cref="NullableFFHandle{T}"/> to a raw <see langword="T*"/>
    /// </summary>
    /// <remarks>
    /// Useful when passing params to native <see cref="FFmpeg"/> functions that accept null pointers
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe implicit operator T*(NullableFFHandle<T> handle) => handle._handle;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe implicit operator NullableFFHandle<T>(T* handle) => new(handle);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator NullableFFHandle<T>(FFHandle<T> handle)
    {
        unsafe
        {
            return handle.Raw;
        }
    }
}