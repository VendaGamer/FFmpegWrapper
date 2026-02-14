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
                return _handle;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected FFObject()
    {
        
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
    public static unsafe implicit operator HandleSource<TRaw>(FFObject<TRaw> ownedObject)
    {
        fixed(TRaw** ptr = &ownedObject._handle)
            return ptr;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe implicit operator TRaw**(FFObject<TRaw> ownedObject)
    {
        HandleSource<TRaw> source = ownedObject;
        return source;
    }
}