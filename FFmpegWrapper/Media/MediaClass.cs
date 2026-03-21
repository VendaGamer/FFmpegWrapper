namespace FFmpegWrapper.Media;

public readonly struct MediaClass : IHandleObserver<AVClass>
{
    public Handle<AVClass> Handle {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            unsafe {
                return (Handle<AVClass>)_handle;
            }
        }
    }
    
    
    internal readonly unsafe AVClass* _handle;

    public MediaClass(Handle<AVClass> handle)
    {
        unsafe {
            _handle = handle;
        }
    }
}