namespace FFmpegWrapper.Media.Formats;

using System.Runtime.ConstrainedExecution;

using Extensions;

public readonly struct ChannelLayout : IFFWrapped<AVChannelLayout>, IEquatable<ChannelLayout>
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

    internal unsafe AVChannelLayout* Handle {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            fixed (AVChannelLayout* layout = &Native) {
                return layout;
            }
        }
    }

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
        
    AVChannelLayout IFFWrapped<AVChannelLayout>.Native
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
            return av_channel_layout_channel_from_index(Handle, index);
        }
    }


    public AVChannel this[uint index] {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            unsafe
            {
                return av_channel_layout_channel_from_index(Handle, index);
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
            av_channel_layout_default(layout.Handle, numChannels);
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
            if (av_channel_layout_from_mask(layout.Handle, mask) < 0) {
                throw new ArgumentException();
            }
            return layout;
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ChannelLayout FromString(ReadOnlySpan<byte> str)
    {
        unsafe
        {
            ChannelLayout layout = default;
            if (av_channel_layout_from_string(layout.Handle, str.RawHandle) < 0) {
                throw new ArgumentException(nameof(str));
            }
            return layout;
        }
    }
    

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override string ToString()
    {
        unsafe
        {
            var buf = stackalloc byte[128];
            var size = av_channel_layout_describe(Handle, buf, 128).CheckError();
            return FFHelper.SpanToStringUtf8(new ReadOnlySpan<byte>(buf, size - 1));
        }
    }

    /// <inheritdoc />
    public bool Equals(ChannelLayout other)
    {
        unsafe
        {
            fixed (AVChannelLayout* a = &Native) {
                var c = av_channel_layout_compare(a, &other.Native);
                return c == 0;
            }
        }
    }


    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is ChannelLayout other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => NumChannels;
}
