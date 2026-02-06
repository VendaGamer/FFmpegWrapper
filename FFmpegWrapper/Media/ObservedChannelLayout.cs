namespace FFmpegWrapper.Media;

public readonly ref struct ObservedChannelLayout : IHandleObserver<AVChannelLayout>
{
    
    #region Properties
    
    Handle<AVChannelLayout> IHandleObserver<AVChannelLayout>.Handle
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
    
    public readonly Handle<AVChannelLayout> Handle;
    
    #region Methods

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CopyTo(Handle<AVChannelLayout> dest)
    {
        unsafe
        {
            av_channel_layout_copy(dest, Handle);
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CopyFrom(Handle<AVChannelLayout> source)
    {
        unsafe
        {
            av_channel_layout_copy(Handle, source);
        }
    }
    
    #endregion
    

}