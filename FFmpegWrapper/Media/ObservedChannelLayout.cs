namespace FFmpegWrapper.Media;

public readonly ref struct ObservedChannelLayout : IFFHandleObserver<AVChannelLayout>
{
    
    #region Properties
    
    FFHandle<AVChannelLayout> IFFHandleObserver<AVChannelLayout>.Handle
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Handle;
    }

    public ChannelLayout ChannelLayout
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(Handle.Ref);
    }

    #endregion
    
    public readonly FFHandle<AVChannelLayout> Handle;
    
    #region Methods

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CopyTo(FFHandle<AVChannelLayout> dest)
    {
        unsafe
        {
            av_channel_layout_copy(dest, Handle);
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CopyFrom(FFHandle<AVChannelLayout> source)
    {
        unsafe
        {
            av_channel_layout_copy(Handle, source);
        }
    }
    
    #endregion
    

}