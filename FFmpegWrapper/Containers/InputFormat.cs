namespace FFmpegWrapper.Containers;

public readonly struct InputFormat : IFFHandle<AVInputFormat>
{
    public FFHandle<AVInputFormat> Handle {
        get {
            unsafe
            {
                return _handle;
            }
        }
    }


    private readonly unsafe AVInputFormat* _handle;
    
    private unsafe InputFormat(AVInputFormat* handle)
    {
        Handle.Ref.
        _handle = handle;
    }

    // Factory method to create from handle
    public static unsafe InputFormat FromHandle(AVInputFormat* handle)
    {
        if (handle == null)
            throw new ArgumentNullException(nameof(handle));
        
        return new InputFormat(handle);
    }
}