namespace FFmpegWrapper.Core;

public class FFBufferObject<TRaw> : FFObjectBase<TRaw>
    where TRaw : unmanaged
{
    public override Handle<TRaw> Handle {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Buffer.Data;
    }

    public MediaBuffer<TRaw> Buffer {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            unsafe {
                if (_handle is null)
                    throw new ObjectDisposedException(nameof(TRaw));
                
                return new MediaBuffer<TRaw>(
                    new Handle<AVBufferRef>(_handle, new SkipValidation()
                ));
            }
        }
    }
    
    internal unsafe AVBufferRef* _handle;

    protected override void Free()
    {
        
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe implicit operator HandleSource<AVBufferRef>(FFBufferObject<TRaw> ownedObject)
    {
        fixed(AVBufferRef** ptr = &ownedObject._handle)
            return ptr;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe implicit operator AVBufferRef**(FFBufferObject<TRaw> ownedObject)
    {
        HandleSource<AVBufferRef> source = ownedObject;
        return source;
    }
}