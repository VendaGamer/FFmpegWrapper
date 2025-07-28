namespace FFmpegWrapper.Containers;

public readonly struct InputFormat : IHandle<AVInputFormat>
{
    unsafe AVInputFormat* IHandle<AVInputFormat>.Handle => handle;
    
    
    
    private readonly unsafe AVInputFormat* handle;
    
    private unsafe InputFormat(AVInputFormat* handle)
    {
        this.handle = handle;
    }

    // Factory method to create from handle
    public static unsafe InputFormat FromHandle(AVInputFormat* handle)
    {
        if (handle == null)
            throw new ArgumentNullException(nameof(handle));
        
        return new InputFormat(handle);
    }
}