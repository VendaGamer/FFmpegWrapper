namespace FFmpegWrapper;

public readonly struct MediaBuffer<TRaw> : IHandleObserver<AVBufferRef>
    where TRaw : unmanaged
{
    public Handle<AVBufferRef> Handle {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            unsafe {
                return _handle;
            }
        }
    }

    public Handle<TRaw> Data {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            unsafe {
                return (TRaw*)Handle.Ref.data;
            }
        }
    }

    internal readonly unsafe AVBufferRef* _handle;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal MediaBuffer(Handle<AVBufferRef> handle)
    {
        unsafe {
            _handle = handle;
        }
    }
}
