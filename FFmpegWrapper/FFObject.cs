namespace FFmpeg.Wrapper;

/// <summary>
/// Base class for all unmanaged FFmpeg objects
/// </summary>
public abstract class FFObject<T> : IDisposable, IHandle<T> where T : unmanaged
{
    /// <summary>
    /// Handle to unmanaged FFmpeg object
    /// </summary>
    public abstract unsafe T* Handle { get; }
    /// <summary>
    /// Frees all unmanaged objects out of the memory
    /// </summary>
    public void Dispose()
    {
        Free();
        GC.SuppressFinalize(this);
    }
    ~FFObject() => Free();
    
    /// <summary>
    /// Frees all unmanaged objects out of the memory
    /// </summary>
    protected abstract void Free();
    
    protected unsafe virtual void ThrowIfDisposed()
    {
        if (Handle == null) {
            throw new ObjectDisposedException(nameof(this.GetType));
        }
    }
    
}