namespace FFmpegWrapper;

public readonly struct OptionDefaultValue : IFFWrapped<AVOption_default_val>
{
    public FFHandle<AVOption_default_val> Handle {
        get {
            unsafe
            {
                return _handle;
            }
        }
    }

    private readonly unsafe AVOption_default_val* _handle;


    public AVOption_default_val Native { get; }
}