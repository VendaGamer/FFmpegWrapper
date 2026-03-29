namespace FFmpegWrapper.Core;

using System.Runtime.InteropServices;

public abstract class FFObject<TRaw> : FFObjectBase<TRaw>, IHandleSource<TRaw>
    where TRaw : unmanaged
{
    /// <summary>
    /// pointer of the underlying unmanaged structure.
    /// </summary>
    protected internal unsafe TRaw* _handle;

    public override Handle<TRaw> Handle {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            unsafe {
                if (_handle is null)
                    throw new ObjectDisposedException(nameof(TRaw));

                return WrapperHelper.UnsafeHandle(_handle);
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
            _handle = handle;
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override unsafe ref TRaw* GetPinnableReference()
    {
        return ref _handle;
    }
}