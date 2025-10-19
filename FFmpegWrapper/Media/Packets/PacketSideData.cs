namespace FFmpegWrapper.Media.Packets;

public readonly struct PacketSideData
{

    public FFHandle<AVPacketSideData> Handle {
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
                return new Span<byte>(handle.Ref.data, checked((int)handle.Ref.size));
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

    public PacketSideData(FFHandle<AVPacketSideData> handle)
    {
        unsafe
        {
            _handle = handle;
        }
    }

    public override string ToString() => $"{ffmpeg.av_packet_side_data_name(Type)}: {Size} bytes";
}