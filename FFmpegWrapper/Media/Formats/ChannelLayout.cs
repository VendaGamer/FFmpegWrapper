namespace FFmpegWrapper.Media.Formats;

using System.Runtime.ConstrainedExecution;

using Extensions;

public readonly struct ChannelLayout : IEquatable<ChannelLayout>
{
    internal unsafe AVChannelLayout* Handle {
        get {
            fixed (AVChannelLayout* layout = &Native) {
                return layout;
            }
        }
    }
    
    public readonly AVChannelLayout Native;
    public AVChannelOrder Order => Native.order;

    /// <summary>number of channels</summary>
    public int NumChannels => Native.nb_channels;

    /// <inheritdoc cref="ffmpeg.av_channel_layout_channel_from_index"/>
    public AVChannel GetChannel(uint index)
    {
        unsafe
        {
            return av_channel_layout_channel_from_index(Handle, index);
        }
    }

    public ChannelLayout(AVChannelLayout native)
    {
        Native = native;
    }
    

    /// <summary> Get the default channel layout for a given number of channels. </summary>
    internal static ChannelLayout GetDefault(int numChannels)
    {
        unsafe
        {
            ChannelLayout layout = default;
            av_channel_layout_default(layout.Handle, numChannels);
            return layout;
        }
    }

    /// <summary> Initialize a native channel layout from a bitmask indicating which channels are present. </summary>
    /// <exception cref="ArgumentException"></exception>
    public static ChannelLayout FromMask(ulong mask)
    {
        unsafe
        {
            ChannelLayout layout = default;
            if (av_channel_layout_from_mask(layout.Handle, mask) < 0) {
                throw new ArgumentException();
            }
            return layout;
        }
    }
    
    
    internal static ChannelLayout FromString(ReadOnlySpan<byte> str)
    {
        unsafe
        {
            ChannelLayout layout = default;
            if (av_channel_layout_from_string(layout.Handle, str.RawHandle) < 0) {
                throw new ArgumentException();
            }
            return layout;
        }
    }

    public unsafe void CopyTo(FFHandle<AVChannelLayout> dest)
    {
        av_channel_layout_copy(dest, Handle).CheckError();
    }
    
    public void CopyTo(IFFHandleObserver<AVChannelLayout> dest)
    {
        unsafe
        {
            av_channel_layout_copy(dest.Handle, Handle).CheckError();
        }
    }

    public void CopyFrom(IFFHandleObserver<AVChannelLayout> source)
    {
        unsafe
        {
            av_channel_layout_copy(Handle, source.Handle).CheckError();
        }
    }

    /// <inheritdoc />
    public override string ToString()
    {
        unsafe
        {
            int requiredSize = av_channel_layout_describe(Handle, null, 0).CheckError();
        
            Span<byte> buf = stackalloc byte[requiredSize];
            
            fixed (byte* ptr = buf) {
                av_channel_layout_describe(Handle, ptr, (nuint)requiredSize).CheckError();
            }
            
            return FFHelper.SpanToStringUtf8(buf);
        }
    }

    /// <inheritdoc />
    public bool Equals(ChannelLayout other)
    {
        unsafe
        {
            fixed (AVChannelLayout* a = &Native) {
                int c = av_channel_layout_compare(a, &other.Native);
                return c == 0;
            }
        }
    }


    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is ChannelLayout other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => NumChannels;
}
