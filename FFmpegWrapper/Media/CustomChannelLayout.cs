namespace FFmpegWrapper.Media;

using System.Runtime.ConstrainedExecution;

public sealed class CustomChannelLayout : CriticalFinalizerObject, IDisposable
{
    private const string UnableToCopy = "Unable to copy custom channel layout.";
    
    #region StaticProperties

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Copy(in AVChannelLayout src, ref AVChannelLayout dest)
    {
        unsafe
        {
            fixed(AVChannelLayout* pSrc = &src)
            fixed(AVChannelLayout* pDes = &dest)
                av_channel_layout_copy(pSrc, pDes);
        }
    }
    
    #endregion
    
    #region Properties
    
    public Span<AVChannelCustom> Map
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            unsafe {
                if (Native.u.map is null)
                    return Span<AVChannelCustom>.Empty;

                return new Span<AVChannelCustom>(Native.u.map, Native.nb_channels);
            }
        }
    }

    public ChannelLayout Layout
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(Native);
    }

    #endregion
    
    public readonly AVChannelLayout Native;
    
    #region Constructors
    
    /// <summary>
    /// Initialize a custom channel layout with the specified number of channels.
    /// The channel map will be allocated and the designation of all channels will be set to <see cref="AVChannel.AV_CHAN_UNKNOWN"/>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public CustomChannelLayout(int numChannels)
    {
        unsafe
        {
            fixed(AVChannelLayout* ptr = &Native)
                av_channel_layout_custom_init(ptr, numChannels);
        }
    }
    
    #endregion
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CopyTo(Handle<AVChannelLayout> dest)
    {
        unsafe
        {
            fixed(AVChannelLayout* ptr = &Native)
                av_channel_layout_copy(dest, ptr)
                    .CheckError(UnableToCopy);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CopyFrom(Handle<AVChannelLayout> source)
    {
        unsafe
        {
            fixed(AVChannelLayout* ptr = &Native)
                av_channel_layout_copy(ptr,source)
                    .CheckError(UnableToCopy);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    ~CustomChannelLayout() => Dispose();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose()
    {
        unsafe
        {
            fixed(AVChannelLayout* ptr = &Native)
                av_channel_layout_uninit(ptr);
        }
    }
}
