namespace FFmpegWrapper.Core;

using System.Runtime.InteropServices;

public abstract class FFObject<TRaw> : FFObjectBase<TRaw>
    where TRaw : unmanaged
{
    /// <summary>
    /// pointer of the underlying unmanaged structure.
    /// </summary>
    internal unsafe TRaw* _handle;

    public override Handle<TRaw> Handle {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            unsafe {
                if (_handle is null)
                    throw new ObjectDisposedException(nameof(FFObject<TRaw>));

                return new Handle<TRaw>(_handle, new SkipValidation());
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected FFObject()
    {
        
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected FFObject(NullableHandle<TRaw> handle)
    {
        if (handle.IsNull)
            throw new Exception($"Could not allocate underlying {nameof(TRaw)} structure");

        unsafe {
            _handle = new Handle<TRaw>(handle, new SkipValidation());
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected FFObject(Handle<TRaw> handle)
    {
        unsafe
        {
            _handle = handle;
        }
    }
}