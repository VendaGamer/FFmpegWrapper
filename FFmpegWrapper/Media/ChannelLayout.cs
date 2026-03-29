namespace FFmpegWrapper.Media;

using System.Text;

using FFmpegWrapper.Extensions;

public readonly struct ChannelLayout : IWrapped<AVChannelLayout>, IEquatable<ChannelLayout>
{
    
    #region StaticProperties

    public static ImmutableArray<ChannelLayout> StandardChannelLayouts
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Utils.GetAllStandardChannelLayouts();
    }

    /// <summary>
    /// Workaround class.
    /// Cannot be directly in MediaCodec struct cause of this issue:
    /// https://github.com/dotnet/runtime/issues/104511
    /// </summary>
    private static class Utils
    {
        private static ImmutableArray<ChannelLayout> s_standardChannelLayouts;
            
        public static ImmutableArray<ChannelLayout> GetAllStandardChannelLayouts()
        {
            if (!s_standardChannelLayouts.IsDefault) {
                return s_standardChannelLayouts;
            }
                
            var builder = ImmutableArray.CreateBuilder<ChannelLayout>(37);
                
            unsafe {
                void* opaque = null;
                AVChannelLayout* layout = null;
                
                while ((layout = av_channel_layout_standard(&opaque)) is not null) {

                    AVChannelLayout standard;
                    av_channel_layout_copy(&standard, layout);
                        
                    builder.Add(new ChannelLayout(standard));
                }
            }
                
            s_standardChannelLayouts =  builder.ToImmutable();
            return s_standardChannelLayouts;
        }
    }

    #endregion

    #region Properties

    public AVChannelOrder Order
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Native.order;
    } 

    /// <summary>number of channels</summary>
    public int NumChannels
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Native.nb_channels;
    }
        
    AVChannelLayout IWrapped<AVChannelLayout>.Native
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Native;
    }

    #endregion
    
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public AVChannel GetChannel(uint index)
    {
        unsafe
        {
            var ptr = (AVChannelLayout*)Unsafe.AsPointer(ref Unsafe.AsRef(in Native));
            return av_channel_layout_channel_from_index(ptr, index);
        }
    }


    public AVChannel this[uint index] {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            unsafe
            {
                var ptr = (AVChannelLayout*)Unsafe.AsPointer(ref Unsafe.AsRef(in Native));
                return av_channel_layout_channel_from_index(ptr, index);
            }
        }
    }
    
    public readonly AVChannelLayout Native;

    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// To construct custom ChannelLayout use <see cref="CustomChannelLayout"/>
    /// </remarks>
    /// <param name="native"></param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ChannelLayout(AVChannelLayout native)
    {
        Native = native;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// To construct custom ChannelLayout use <see cref="CustomChannelLayout"/>
    /// </remarks>
    /// <param name="channelOrder"></param>
    /// <param name="channelNum">Number of channels</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ChannelLayout(AVChannelOrder channelOrder, int channelNum, AVChannelFlags flags)
        :this(new AVChannelLayout {
                order = channelOrder,
                nb_channels = channelNum,
                opaque = null,
                u = new AVChannelLayout_u {
                    map = null,
                    mask = (ulong)flags
                }})
    {
        
    }
    
    
    /// <summary> Get the default channel layout for a given number of channels. </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ChannelLayout GetDefault(int numChannels)
    {
        unsafe
        {
            ChannelLayout layout = default;
            av_channel_layout_default(&layout.Native, numChannels);
            return layout;
        }
    }

    /// <summary> Initialize a native channel layout from a bitmask indicating which channels are present. </summary>
    /// <exception cref="ArgumentException"></exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ChannelLayout FromMask(ulong mask)
    {
        unsafe
        {
            ChannelLayout layout = default;
            if (av_channel_layout_from_mask(&layout.Native, mask) < 0)
                throw new ArgumentException(nameof(mask));
            
            return layout;
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ChannelLayout FromString(ReadOnlySpan<byte> str)
    {
        unsafe
        {
            ChannelLayout layout = default;
            if (av_channel_layout_from_string(&layout.Native, str.RawHandle) < 0)
                throw new ArgumentException(nameof(str));
            
            return layout;
        }
    }
    

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override string ToString()
    {
        unsafe {
            var buf = stackalloc byte[128];
            var ptr = (AVChannelLayout*)Unsafe.AsPointer(ref Unsafe.AsRef(in Native));
            var size = av_channel_layout_describe(ptr, buf, 128).CheckError();

            return Encoding.UTF8.GetString(buf, size - 1);
        }
    }

    /// <inheritdoc />
    public bool Equals(ChannelLayout other)
    {
        unsafe
        {
            var ptr = (AVChannelLayout*)Unsafe.AsPointer(ref Unsafe.AsRef(in Native));
            return av_channel_layout_compare(ptr, &other.Native) is 0;
        }
    }


    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is ChannelLayout other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Native.order);
        hash.Add(Native.nb_channels);

        if (Native.order == AVChannelOrder.AV_CHANNEL_ORDER_NATIVE)
        {
            hash.Add(Native.u.mask);
        }
        else
        {
            for (int i = 0; i < Native.nb_channels; i++)
            {
                unsafe {
                    hash.Add(Native.u.map[i].id);
                }
            }
        }

        return hash.ToHashCode();
    }
}
