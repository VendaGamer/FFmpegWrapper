namespace FFmpegWrapper.Containers;

public struct OptionValue : IFFHandleObserver<AVOption_u>
{
    public FFHandle<AVOption_u> Handle {
        get {
            unsafe {
                return _handle;
            }
        }
    }
    
    

    internal readonly unsafe AVOption_u* _handle;

    public OptionValue(FFHandle<AVOption_u> handle)
    {
        unsafe {
            _handle = handle;
        }
    }
}