namespace FFmpegWrapper.Media;

using System.Runtime.ConstrainedExecution;

public sealed class CustomChannelLayout
    : CriticalFinalizerObject, IFFHandleOwner<AVChannelLayout>
{

    #region StaticProperties

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Copy(FFHandle<AVChannelLayout> src, FFHandle<AVChannelLayout> dest)
    {
        unsafe
        {
            av_channel_layout_copy(src, dest);
        }
    }
    
    #endregion
    
    #region Properties
    
    public ChannelCustom Custom
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            unsafe
            {
                return new ChannelCustom(_native.u.map);
            }
        }
    }

    public ChannelLayout Layout
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(_native);
    }

    public FFHandle<AVChannelLayout> Handle
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            unsafe
            {
                if(_native.u.map is null)
                    throw new Exception("Cannot borrow uninitialized custom channel layout.");
                
                fixed(void* ptr = &_native)
                    return new FFHandle<AVChannelLayout>(ptr);
            }
        }
    }

    #endregion
    
    private readonly AVChannelLayout _native;
    
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
            fixed(AVChannelLayout* ptr = &_native)
                av_channel_layout_custom_init(ptr, numChannels);
        }
    }
    
    #endregion
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CopyTo(FFHandle<AVChannelLayout> dest)
    {
        unsafe
        {
            av_channel_layout_copy(dest, Handle).CheckError("Unable to copy custom channel layout.");
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CopyFrom(FFHandle<AVChannelLayout> source)
    {
        unsafe
        {
            av_channel_layout_copy(Handle, source).CheckError("Unable to copy custom channel layout.");
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    ~CustomChannelLayout() => Dispose();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose()
    {
        unsafe
        {
            fixed(AVChannelLayout* ptr = &_native)
                av_channel_layout_uninit(ptr);
        }
    }
}
