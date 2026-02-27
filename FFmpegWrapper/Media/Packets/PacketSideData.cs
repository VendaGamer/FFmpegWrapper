namespace FFmpegWrapper.Media.Packets;

using System.Runtime.InteropServices;

public readonly struct PacketSideData
{

    public Handle<AVPacketSideData> Handle {
        get {
            unsafe
            {
                return _handle;
            }
        }
    }

    public Span<byte> Data {
        get {
            unsafe {
                var handle = Handle;
                return new Span<byte>(handle.Ref.data, (int)handle.Ref.size);
            }
        }
    }

    public ulong Size => Handle.Ref.size;
    public AVPacketSideDataType Type => Handle.Ref.type;

    private readonly unsafe AVPacketSideData* _handle;
    
    public byte this[ulong index] {
        get {
            unsafe {
                var handle = Handle;
                
                if (index > handle.Ref.size) {
                    throw new IndexOutOfRangeException();
                }

                return Handle.Ref.data[index];
            }
        }
    }

    public PacketSideData(Handle<AVPacketSideData> handle)
    {
        unsafe
        {
            _handle = handle;
        }
    }

    public override string ToString()
    {
        unsafe
        {
            return $"{FFHelper.PtrToStringUtf8(av_packet_side_data_name(Type))}: {Size} bytes";
        }
    }
}
