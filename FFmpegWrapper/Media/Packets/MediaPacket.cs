namespace FFmpegWrapper.Media.Packets;

using Streams;

public unsafe class MediaPacket : FFObject<AVPacket>
{
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
        get => Helpers.GetPTS(handle->pts);
        set => Helpers.SetPTS(ref handle->pts, value);
    }
    public long? DecompressionTimestamp {
        get => Helpers.GetPTS(handle->dts);
        set => Helpers.SetPTS(ref handle->dts, value);
    }

    /// <summary> Duration of this packet in <see cref="MediaStream.TimeBase"/> units, 0 if unknown. Equals next_pts - this_pts in presentation order.  </summary>
    public long Duration {
        get => handle->duration;
        set => handle->duration = value;
    }
    public int StreamIndex {
        get => handle->stream_index;
        set => handle->stream_index = value;
    }

    /// <summary> Whether this packet contains a key-frame. (Checks if AV_PKT_FLAG_KEY is set) </summary>
    public bool IsKeyFrame {
        get => (handle->flags & ffmpeg.AV_PKT_FLAG_KEY) != 0;
        set => handle->flags = value ? (handle->flags | ffmpeg.AV_PKT_FLAG_KEY) : (handle->flags & ~ffmpeg.AV_PKT_FLAG_KEY);
    }

    /// <inheritdoc cref="AVPacket.pos"/>
    public long BytePosition {
        get => handle->pos;
        set => handle->pos = value;
    }

    public Span<byte> Data {
        get => new(handle->data, handle->size);
    }

    public PacketSideDataList SideData => new(&handle->side_data, &handle->side_data_elems);

    public MediaPacket()
    {
        handle = ffmpeg.av_packet_alloc();

        if (handle == null) {
            throw new OutOfMemoryException();
        }
    }
    public MediaPacket(int size)
        : this()
    {
        ffmpeg.av_new_packet(handle, size).CheckError("Failed to allocate packet buffer");
    }

    /// <summary> Copies the specified data span to the packet, ensuring buffer space. </summary>
    public void SetData(ReadOnlySpan<byte> data)
    {
        ThrowIfDisposed();

        if (handle->buf == null || handle->buf->size < (ulong)data.Length + ffmpeg.AV_INPUT_BUFFER_PADDING_SIZE) {
            byte* buffer = (byte*)ffmpeg.av_malloc((ulong)data.Length + ffmpeg.AV_INPUT_BUFFER_PADDING_SIZE);
            if (buffer == null) {
                throw new OutOfMemoryException();
            }
            ffmpeg.av_packet_from_data(handle, buffer, data.Length).CheckError("Failed to allocate packet buffer");
        }
        handle->size = data.Length;
        data.CopyTo(Data);
    }

    /// <inheritdoc cref="ffmpeg.av_packet_rescale_ts(AVPacket*, AVRational, AVRational)"/>
    public void RescaleTS(Rational sourceBase, Rational destBase)
    {
        ffmpeg.av_packet_rescale_ts(Handle, sourceBase, destBase);
    }

    /// <summary> Returns the underlying packet pointer after calling av_packet_unref() on it. </summary>
    public AVPacket* UnrefAndGetHandle()
    {
        ThrowIfDisposed();

        ffmpeg.av_packet_unref(handle);
        return handle;
    }

    protected override void Free()
    {
        fixed (AVPacket** pkt = &handle) {
            ffmpeg.av_packet_free(pkt);
        }
    }
}
