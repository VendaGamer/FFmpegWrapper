namespace FFmpegWrapper.Media.Packets;

using Abstractions;

using Core.Flags;

using Streams;

public class MediaPacket : FFObject<AVPacket>
{
    /// <summary>This value is used to represent that no timestamp exists</summary>
    public const long NoTimeStampValue = long.MinValue;
    
    /// <summary>
    /// Presentation timestamp in <see cref="MediaStream.TimeBase"/> units; 
    /// the time at which the decompressed packet will be presented to the user. <br/>
    /// 
    /// Can be <see langword="null"/> if it is not stored in the file. MUST be larger
    /// or equal to <see cref="DecompressionTimestamp"/> as presentation cannot happen before
    /// decompression, unless one wants to view hex dumps.  <br/>
    /// 
    /// Some formats misuse the terms dts and pts/cts to mean something different.
    /// Such timestamps must be converted to true pts/dts before they are stored in AVPacket.
    /// </summary>
    public long? PresentationTimestamp {
        get {
            unsafe
            {
                if(_handle->pts is NoTimeStampValue)
                    return null;
                
                return _handle->pts;
            }
        }
        set {
            unsafe
            {
                if (value is null)
                    _handle->pts = NoTimeStampValue;
                else
                    _handle->pts = value.Value;
            }
        }
    }

    public long? DecompressionTimestamp {
        get {
            ThrowIfDisposed();
            unsafe
            {
                if(_handle->dts is NoTimeStampValue)
                    return null;
                
                return _handle->dts;
            }
        }
        set {
            ThrowIfDisposed();
            unsafe
            {
                if (value is null)
                    _handle->dts = NoTimeStampValue;
                else
                    _handle->dts = value.Value;
            }
        }
    }

    /// <summary> Duration of this packet in <see cref="MediaStream.TimeBase"/> units, 0 if unknown. Equals next_pts - this_pts in presentation order.  </summary>
    public long Duration {
        get => Handle.Ref.duration;
        set => Handle.Ref.duration = value;
    }
    public int StreamIndex {
        get => Handle.Ref.stream_index;
        set => Handle.Ref.stream_index = value;
    }
    
    public PacketFlags Flags {
        get => (PacketFlags)Handle.Ref.flags;
        set => Handle.Ref.flags = (int)value;
    }

    /// <inheritdoc cref="AVPacket.pos"/>
    public long BytePosition {
        get => Handle.Ref.pos;
        set => Handle.Ref.pos = value;
    }

    public Span<byte> Data {
        get {
            unsafe
            {
                ref var handle = ref Handle.Ref;

                return new Span<byte>(handle.data, handle.size);
            }
        }
    }

    internal unsafe byte* DataRaw {
        get => _handle->data;
    }

    internal int DataLength {
        get {
            unsafe
            {
                return _handle->size;
            }
        }
    }

    public PacketSideDataList SideData {
        get {
            unsafe
            {
                return new PacketSideDataList(&_handle->side_data, &_handle->side_data_elems);
            }
        }
    }

    public MediaPacket()
    {
        unsafe
        {
            _handle = ffmpeg.av_packet_alloc();
            if (_handle == null) {
                throw new OutOfMemoryException();
            }
        }
    }
    public MediaPacket(int size)
        : this()
    {
        unsafe
        {
            ffmpeg.av_new_packet(_handle, size).CheckError("Failed to allocate packet buffer");
        }
    }

    /// <summary> Copies the specified data span to the packet, ensuring buffer space. </summary>
    public void SetData(ReadOnlySpan<byte> data)
    {
        unsafe
        {
            ThrowIfDisposed();

            if (_handle->buf == null || _handle->buf->size < (ulong)data.Length + ffmpeg.AV_INPUT_BUFFER_PADDING_SIZE) {
                byte* buffer = (byte*)ffmpeg.av_malloc((ulong)data.Length + ffmpeg.AV_INPUT_BUFFER_PADDING_SIZE);
                if (buffer == null) {
                    throw new OutOfMemoryException();
                }
                ffmpeg.av_packet_from_data(_handle, buffer, data.Length).CheckError("Failed to allocate packet buffer");
            }
            _handle->size = data.Length;
            data.CopyTo(Data);
        }
    }

    /// <inheritdoc cref="ffmpeg.av_packet_rescale_ts(AVPacket*, AVRational, AVRational)"/>
    public void RescaleTS(Rational sourceBase, Rational destBase)
    {
        unsafe
        {
            ffmpeg.av_packet_rescale_ts(Handle, sourceBase, destBase);
        }
    }
    
    public FFHandle<AVPacket> Clear()
    {
        ThrowIfDisposed();
        unsafe
        {
            ffmpeg.av_packet_unref(_handle);
            return _handle;
        }
    }

    protected override void Free()
    {
        unsafe
        {
            fixed (AVPacket** pkt = &_handle) {
                ffmpeg.av_packet_free(pkt);
            }
        }
    }
}
