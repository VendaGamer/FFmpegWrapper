namespace FFmpegWrapper.Core;


/// <summary>
/// Provides a base implementation for managed wrapper classes that encapsulate unmanaged FFmpeg objects.
/// This abstract class handles the common patterns of resource management, disposal, and safe access
/// to underlying FFmpeg structures while implementing the <see cref="IHandle{T}"/> interface.
/// </summary>
/// <typeparam name="TRaw">
/// The FFmpeg.AutoGen unmanaged structure type that this wrapper encapsulates.
/// Must be an unmanaged type (value type with no managed references).
/// </typeparam>
/// <remarks>
/// <para>
/// This base should be used for optional owning of other <see cref="FFObject{T}"/>
/// Can be useful for owning IOContext for example
/// </para>
/// </remarks>
public abstract class FFObjectOwner<TRaw> : IDisposable, IHandle<TRaw> where TRaw : unmanaged
{
    private ImmutableArray<IDisposable> _ownedObjects = ImmutableArray<IDisposable>.Empty;
    private bool _disposed;
    /// <value>
    /// A pointer to the native FFmpeg structure of type <typeparamref name="TRaw"/>.
    /// </value>
    protected unsafe TRaw* handle;

    internal unsafe TRaw* Handle {
        get {
            ThrowIfDisposed();
            return handle;
        }
    }
    
    bool IHandle<TRaw>.IsValid {
        get {
            unsafe
            {
                return handle is not null;
            }
        }
    }

    /// <summary>
    /// Gets a pointer to the underlying unmanaged FFmpeg structure.
    /// </summary>
    /// <value>
    /// A pointer to the native FFmpeg structure of type <typeparamref name="TRaw"/>.
    /// Throws if the object has been disposed.
    /// </value>
    /// <returns>
    /// A pointer to the unmanaged FFmpeg structure, or null if disposed.
    /// </returns>
    unsafe TRaw* IHandle<TRaw>.Handle => handle;

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
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (_disposed) return;
        if (disposing) {
            
            foreach (var owned in _ownedObjects) {
                owned.Dispose();
            }
        }
        
        Free();

        _disposed = true;
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
    ~FFObjectOwner() => Dispose(false);
    
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
    protected abstract void Free();
    
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
    protected unsafe void ThrowIfDisposed()
    {
        if (handle == null) {
            throw new ObjectDisposedException($"The underlying unmanaged {typeof(TRaw).Name} has been disposed. ");
        }
    }
    /// <summary>
    /// Takes ownership of managed object.
    /// When disposing disposes of the given object
    /// </summary>
    protected void TakeOwnership(IDisposable objectToOwn)
    {
        _ownedObjects = _ownedObjects.Add(objectToOwn);
    }
    /// <summary>
    /// Takes ownership of managed objects.
    /// When disposing disposes of the given objects
    /// </summary>
    protected void TakeOwnership(params ReadOnlySpan<IDisposable> objectsToOwn)
    {
        _ownedObjects = _ownedObjects.AddRange(objectsToOwn);
    }
}