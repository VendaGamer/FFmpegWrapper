namespace FFmpegWrapper.Media;

public readonly struct ChannelCustom(FFHandle<AVChannelCustom> handle) : IFFHandleObserver<AVChannelCustom>
{

    #region Properties

    public unsafe FFHandle<AVChannelCustom> Handle => new(_handle);

    public ref AVChannel Channel => ref Handle.Ref.id;

    public unsafe ReadOnlySpan<byte> Name => new(&Handle.Raw->name._0, 16);
    
    /// <summary>User data</summary>
    public unsafe ref void* Opaque => ref Handle.Ref.opaque;

    #endregion
    
    internal readonly unsafe AVChannelCustom* _handle = handle;
    
    
    
}