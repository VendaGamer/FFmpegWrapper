namespace FFmpegWrapper.Media.Packets;

using Core;
using Streams;

public class MediaPacket : FFObject<AVPacket>
{
    
    #region Properties
    
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            ref var handle = ref Handle.Ref;
            
            if(handle.pts is AV_NOPTS_VALUE)
                return null;
            
            return handle.pts;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => Handle.Ref.pts = value ?? AV_NOPTS_VALUE;
    }

    public long? DecompressionTimestamp {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            ref var handle = ref Handle.Ref;
            
            if(handle.dts is AV_NOPTS_VALUE)
                return null;
            
            return handle.dts;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => Handle.Ref.dts = value ?? AV_NOPTS_VALUE;
    }

    /// <summary> Duration of this packet in <see cref="MediaStream.TimeBase"/> units, 0 if unknown. Equals next_pts - this_pts in presentation order.  </summary>
    public long Duration {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Handle.Ref.duration;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => Handle.Ref.duration = value;
    }
    public int StreamIndex {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Handle.Ref.stream_index;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => Handle.Ref.stream_index = value;
    }
    
    public ref AV_PKT_FLAGS Flags => ref Unsafe.As<int, AV_PKT_FLAGS>(ref Handle.Ref.flags);

    /// <inheritdoc cref="AVPacket.pos"/>
    public ref long BytePosition => ref Handle.Ref.pos;

    public Span<byte> Data {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            unsafe {
                ref var handle = ref Handle.Ref;
                
                return new Span<byte>(handle.data, handle.size);
            }
        }
    }
    
    public unsafe byte* DataRaw => Handle.Ref.data;
    
    public int DataLength => Handle.Ref.size;

    public PacketSideDataList SideData {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            unsafe
            {
                var handle = Handle.Raw;
                
                return new PacketSideDataList(&handle->side_data, &_handle->side_data_elems);
            }
        }
    }
    
    #endregion
    
    
    #region Constructors

    public MediaPacket(Handle<AVPacket> handle) : base(handle) { }
    
    public unsafe MediaPacket() : base(av_packet_alloc()) { }
    
    public MediaPacket(int size) : this()
    {
        unsafe
        {
            av_new_packet(_handle, size).CheckError("Failed to allocate packet buffer");
        }
    }
    
    #endregion

    

    /// <summary> Copies the specified data span to the packet, ensuring buffer space. </summary>
    public void SetData(ReadOnlySpan<byte> data)
    {
        unsafe
        {
            ThrowIfDisposed();

            if (_handle->buf == null || _handle->buf->size < (ulong)data.Length + AV_INPUT_BUFFER_PADDING_SIZE) {
                var buffer = (byte*)av_malloc((nuint)data.Length + AV_INPUT_BUFFER_PADDING_SIZE);
                if (buffer == null) {
                    throw new OutOfMemoryException();
                }
                av_packet_from_data(_handle, buffer, data.Length).CheckError("Failed to allocate packet buffer");
            }
            _handle->size = data.Length;
            data.CopyTo(Data);
        }
    }

    /// <inheritdoc cref="ffmpeg.av_packet_rescale_ts(AVPacket*, AVRational, AVRational)"/>
    public unsafe void RescaleTS(Rational sourceBase, Rational destBase)
        => av_packet_rescale_ts(Handle, sourceBase, destBase);

    public void SaveData(string fileName)
    {
    #if NET6_0_OR_GREATER
        using var handle = File.OpenHandle(fileName, FileMode.Create, FileAccess.Write);
        RandomAccess.Write(handle, Data, 0);
    #elif NETSTANDARD2_1_OR_GREATER
        using var fs = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None);
        fs.Write(Data);
    #else
        using var fs = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None);
        unsafe {
            using var us = new UnmanagedMemoryStream(DataRaw, DataLength);
            us.CopyTo(fs);
        }

    #endif
    }
    
    public unsafe void Clear()
    {
        ThrowIfDisposed();
        av_packet_unref(_handle);
    }

    protected unsafe override void Free()
    {
        fixed (AVPacket** pkt = &_handle) {
            av_packet_free(pkt);
        }
    }
}
