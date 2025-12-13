namespace FFmpegWrapper.Media;

using System.Buffers;
using System.Runtime.ConstrainedExecution;

public sealed class CustomChannelLayout : CriticalFinalizerObject, IDisposable
{
    public readonly ChannelLayout Layout;
    
    internal CustomChannelLayout(ChannelLayout layout)
    {
        Layout = layout;
    }

    public static CustomChannelLayout DefaultFor(int numChannels)
    {
        return new CustomChannelLayout(ChannelLayout.GetDefault(numChannels));
    }

    public static CustomChannelLayout FromString(ReadOnlySpan<byte> str)
    {
        return new CustomChannelLayout(ChannelLayout.FromString(str));
    }

    ~CustomChannelLayout() => Dispose(false);

    public void Dispose() => Dispose(true);

    private unsafe void Dispose(bool disposing)
    {
        av_channel_layout_uninit(Layout.Handle);
    }
}
