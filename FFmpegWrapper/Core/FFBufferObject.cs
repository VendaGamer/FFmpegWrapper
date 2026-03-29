namespace FFmpegWrapper.Core;

public class FFBufferObject<TRaw> : FFObjectBase<TRaw>
    where TRaw : unmanaged
{
    public override Handle<TRaw> Handle {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            unsafe
            {
                if (_handle is null)
                    throw new ObjectDisposedException(nameof(TRaw));
                
                return WrapperHelper.UnsafeHandle((TRaw*)_handle->data);
            }
        }
    }

    public MediaBuffer<TRaw> Buffer {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            unsafe {
                if (_handle is null)
                    throw new ObjectDisposedException(nameof(TRaw));
                
                return new MediaBuffer<TRaw>(WrapperHelper.UnsafeHandle(_handle));
            }
        }
    }
    
    internal unsafe AVBufferRef* _handle;

    protected override void Free()
    {
        
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override unsafe ref TRaw* GetPinnableReference()
    {
        ThrowIfDisposed();
        return ref *(TRaw**)&_handle->data;
    }
}