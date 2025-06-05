namespace FFmpeg.Wrapper;

/// <summary>
/// Provides a base implementation for managed wrapper classes that encapsulate unmanaged FFmpeg objects.
/// This abstract class handles the common patterns of resource management, disposal, and safe access
/// to underlying FFmpeg structures while implementing the <see cref="IHandle{T}"/> interface.
/// </summary>
/// <typeparam name="T">
/// The FFmpeg.AutoGen unmanaged structure type that this wrapper encapsulates.
/// Must be an unmanaged type (value type with no managed references).
/// </typeparam>
/// <remarks>
/// <para>
/// This class implements the Dispose pattern to ensure proper cleanup of unmanaged FFmpeg resources.
/// Derived classes must implement the <see cref="Free"/> method to perform the actual resource cleanup
/// specific to their FFmpeg object type.
/// </para>
/// </remarks>
public abstract class FFObject<T> : IDisposable, IHandle<T> where T : unmanaged
{
    /// <summary>
    /// pointer to the underlying unmanaged FFmpeg structure.
    /// </summary>
    protected unsafe T* _handle;

    /// <summary>
    /// Gets a pointer to the underlying unmanaged FFmpeg structure.
    /// </summary>
    /// <value>
    /// A pointer to the native FFmpeg structure of type <typeparamref name="T"/>.
    /// Throws if the object has been disposed.
    /// </value>
    /// <returns>
    /// A pointer to the unmanaged FFmpeg structure, or null if disposed.
    /// </returns>
    public unsafe T* Handle {
        get {
            ThrowIfDisposed();
            return _handle;
        }
    }


    /// <summary>
    /// Releases all resources used by the FFmpeg object and suppresses finalization.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method implements the standard Dispose pattern by calling <see cref="Free"/>
    /// and then suppressing finalization to prevent the finalizer from running.
    /// </para>
    /// <para>
    /// After calling Dispose, the object should not be used again. Subsequent calls
    /// to methods that access the <see cref="Handle"/> will throw <see cref="ObjectDisposedException"/>.
    /// </para>
    /// <para>
    /// This method is safe to call multiple times. Subsequent calls after the first
    /// will have no effect.
    /// </para>
    /// </remarks>
    public void Dispose()
    {
        Free();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Finalizer that ensures unmanaged resources are freed if Dispose was not called.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This finalizer provides a safety net to ensure that unmanaged FFmpeg resources
    /// are eventually freed even if the client code fails to call <see cref="Dispose"/>.
    /// However, relying on finalization can lead to delayed resource cleanup and
    /// potential resource exhaustion.
    /// </para>
    /// <para>
    /// Best Practice:
    /// Always call <see cref="Dispose"/> explicitly or use the object within a
    /// using statement to ensure timely resource cleanup.
    /// </para>
    /// </remarks>
    ~FFObject() => Free();
    
    /// <summary>
    /// Releases the unmanaged FFmpeg resources associated with this object.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Derived classes must implement this method to perform the actual cleanup
    /// of their specific FFmpeg resources. This typically involves calling the
    /// appropriate FFmpeg cleanup function.
    /// </para>
    /// <para>
    /// This method may be called from both <see cref="Dispose"/> and the finalizer,
    /// so it should not access other managed objects that might have been finalized.
    /// </para>
    /// </remarks>
    protected unsafe abstract void Free();
    
    /// <summary>
    /// Throws an <see cref="ObjectDisposedException"/> if this object has been disposed.
    /// </summary>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the object has been disposed (i.e., when <see cref="Handle"/> returns null).
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method should be called at the beginning of public methods that access
    /// the underlying FFmpeg structure to ensure the object is still valid for use.
    /// </para>
    /// </remarks>
    protected unsafe virtual void ThrowIfDisposed()
    {
        if (_handle == null) {
            throw new ObjectDisposedException(GetType().Name);
        }
    }
}