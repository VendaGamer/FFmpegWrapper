namespace FFmpegWrapper.Media.Packets;

using System.Text;
using CommunityToolkit.HighPerformance;

public readonly ref struct PacketSideDataList
{
    public HandleSource<AVPacketSideData> Handle {
        get {
            unsafe
            {
                return _handle;
            }
        }
    }

    public int Count {
        get {
            unsafe
            {
                return (*_count);
            }
        }
    }

    public PacketSideData this[int index] {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            unsafe
            {
                if ((uint)index >= (uint)_count) {
                    throw new IndexOutOfRangeException();
                }

                return new PacketSideData(
                    new Handle<AVPacketSideData>(
                        _handle[index],
                        new SkipValidation()
                    )
                );
            }
        }
    }
    
    private readonly unsafe AVPacketSideData** _handle;
    private readonly unsafe int* _count;

    public PacketSideDataList(HandleSource<AVPacketSideData> entries, Handle<int> count)
    {
        unsafe
        {
            _handle = entries;
            _count = count;
        }
    }

    /// <summary> Returns the side data entry for the given type, or null if not present. </summary>
    public bool TryGet(AVPacketSideDataType type, out PacketSideData sideData)
    {
        unsafe
        {
            var entry = av_packet_side_data_get(*_handle, *_count, type);
            if (entry is null) {
                sideData = default;
                return false;
            }

            sideData = new PacketSideData(
                new Handle<AVPacketSideData>(
                    entry,
                    new SkipValidation()
                )
            );
            
            return true;
        }
    }

    /// <summary> Allocates or overwrites a side data entry. </summary>
    public PacketSideData Add(AVPacketSideDataType type, nuint size)
    {
        unsafe
        {
            var entry = av_packet_side_data_new(_handle, _count, type, size, 0);
            if (entry is null)
                throw new Exception("Unable to allocate side data");
            
            return new PacketSideData(
                new Handle<AVPacketSideData>(
                    entry,
                    new SkipValidation()
                )
            );
        }
    }

    public bool Remove(AVPacketSideDataType type)
    {
        unsafe
        {
            int prevCount = Count;
            av_packet_side_data_remove(*_handle, _count, type);
            return Count != prevCount;
        }
    }

    public void Clear()
    {
        // https://github.com/FFmpeg/FFmpeg/blob/4e120fbbbd087c3acbad6ce2e8c7b1262a5c8632/libavfilter/f_sidedata.c#L117
        while (Count != 0) {
            unsafe
            {
                av_packet_side_data_remove(*_handle, _count, _handle[0]->type);
            }
        }
    }

    /// <summary> Returns the value of an <see cref="AVPacketSideDataType.AV_PKT_DATA_DISPLAYMATRIX"/> entry. </summary>
    public bool TryGetDisplayMatrix(out ReadOnlySpan2D<byte> entry)
    {
        entry = default!;
        return false;
    }

    public override string ToString()
    {
        var sb = new StringBuilder("[");

        for (int i = 0; i < Count; i++) {
            if (i != 0) sb.Append(", ");
            sb.Append(this[i].ToString());
        }
        return sb.Append(']').ToString();
    }
}
