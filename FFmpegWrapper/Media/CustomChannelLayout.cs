namespace FFmpegWrapper.Media;

using System.Runtime.ConstrainedExecution;

public sealed class CustomChannelLayout(AVChannelLayout layout)
    : CriticalFinalizerObject, IFFWrapped<AVChannelLayout>, IDisposable
{

    #region StaticProperties

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CustomChannelLayout DefaultFor(int numChannels) => new(ChannelLayout.GetDefault(numChannels).Native);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CustomChannelLayout FromString(ReadOnlySpan<byte> str) => new(ChannelLayout.FromString(str).Native);
    
    

    #endregion
    
    #region Properties
    
    AVChannelLayout IFFWrapped<AVChannelLayout>.Native => Native;
    
    public unsafe ChannelCustom Custom => new(Native.u.map);

    public ChannelLayout Layout => new(Native);

    #endregion

    
    public readonly AVChannelLayout Native = layout;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    ~CustomChannelLayout() => Dispose(false);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose() => Dispose(true);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe void Dispose(bool disposing)
    {
        fixed(AVChannelLayout* ptr = &Native)
            av_channel_layout_uninit(ptr);
    }
}
