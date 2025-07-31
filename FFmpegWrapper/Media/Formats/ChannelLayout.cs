namespace FFmpegWrapper.Media.Formats;

using System.Runtime.ConstrainedExecution;

public readonly struct ChannelLayout : IHandle<AVChannelLayout>, IEquatable<ChannelLayout>
{
    internal unsafe AVChannelLayout* handle {
        get {
            fixed (AVChannelLayout* layout = &Native) {
                return layout;
            }
        }
    }
    
    unsafe AVChannelLayout* IHandle<AVChannelLayout>.Handle => handle;
    public readonly AVChannelLayout Native;
    internal readonly HeapStorage? _heap;
    public ChannelOrder Order {
        get {
            unsafe
            {
                return (ChannelOrder)handle->order;
            }
        }
    }

    /// <summary>number of channels</summary>
    public int NumChannels {
        get {
            unsafe
            {
                return handle->nb_channels;
            }
        }
    }

    /// <inheritdoc cref="ffmpeg.av_channel_layout_channel_from_index"/>
    public AudioChannel GetChannel(uint index)
    {
        unsafe
        {
            return (AudioChannel)ffmpeg.av_channel_layout_channel_from_index(handle, index);
        }
    }

    /// <summary> Get the default channel layout for a given number of channels. </summary>
    public static ChannelLayout GetDefault(int numChannels)
    {
        unsafe
        {
            ChannelLayout layout = default;
            ffmpeg.av_channel_layout_default(layout.handle, numChannels);
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
            if (ffmpeg.av_channel_layout_from_mask(layout.handle, mask) < 0) {
                throw new ArgumentException();
            }
            return layout;
        }
    }

    /// <summary> Initialize a channel layout from a given string description. </summary>
    /// <remarks>
    /// The input string can be represented by:  <br/>
    ///  - the formal channel layout name (returned by ToString()/av_channel_layout_describe()) <br/>
    ///  - single or multiple channel names (returned by av_channel_name(), eg. "FL",
    ///    or concatenated with "+", each optionally containing a custom name after
    ///    a "@", eg. "FL@Left+FR@Right+LFE") <br/>
    ///  - a decimal or hexadecimal value of a native channel layout (eg. "4" or "0x4")  <br/>
    ///  - the number of channels with default layout (eg. "4c")  <br/>
    ///  - the number of unordered channels (eg. "4C" or "4 channels")  <br/>
    ///  - the ambisonic order followed by optional non-diegetic channels (eg. "ambisonic 2+stereo")  <br/>
    /// </remarks>
    public static ChannelLayout FromString(string str)
    {
        unsafe
        {
            ChannelLayout layout = default;
            if (ffmpeg.av_channel_layout_from_string(layout.handle, str) < 0) {
                throw new ArgumentException();
            }
            if (layout.Order == ChannelOrder.Custom) {
                Unsafe.AsRef(in layout._heap) = new HeapStorage() { Data = layout.Native.u.map };
            }
            return layout;
        }
    }

    public unsafe static ChannelLayout FromHandle(AVChannelLayout* layout)
    {
        ChannelLayout dest = default;

        if (layout->order == AVChannelOrder.AV_CHANNEL_ORDER_CUSTOM) {
            ffmpeg.av_channel_layout_copy(dest.handle, layout);
            Unsafe.AsRef(in dest._heap) = new HeapStorage{ Data = dest.Native.u.map };
        } else {
            Unsafe.AsRef(in dest.Native) = *layout;
        }
        return dest;
    }

    public unsafe void CopyTo(AVChannelLayout* dest)
    {
        ffmpeg.av_channel_layout_copy(dest, handle).CheckError();
        GC.KeepAlive(_heap);
    }
    
    public void CopyTo(IHandle<AVChannelLayout> dest)
    {
        unsafe
        {
            ffmpeg.av_channel_layout_copy(dest.Handle, handle).CheckError();
            GC.KeepAlive(_heap);
        }
    }

    /// <inheritdoc />
    public override string ToString()
    {
        unsafe
        {
            int requiredSize = ffmpeg.av_channel_layout_describe(handle, null, 0).CheckError();
        
            Span<byte> buf = stackalloc byte[requiredSize];
            
            fixed (byte* ptr = buf) {
                ffmpeg.av_channel_layout_describe(handle, ptr, (ulong)requiredSize).CheckError();
            }

            return Helpers.SpanToStringUTF8(buf);
        }
    }

    /// <inheritdoc />
    public bool Equals(ChannelLayout other)
    {
        unsafe
        {
            fixed (AVChannelLayout* a = &Native) {
                int c = ffmpeg.av_channel_layout_compare(a, &other.Native);
                GC.KeepAlive(_heap);
                GC.KeepAlive(other._heap);
                return c == 0;
            }
        }
    }
    
    internal unsafe sealed class HeapStorage : CriticalFinalizerObject
    {
        public AVChannelCustom* Data;

        ~HeapStorage()
        {
            ffmpeg.av_freep(Data);
        }
    }


    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is ChannelLayout other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => NumChannels;
}