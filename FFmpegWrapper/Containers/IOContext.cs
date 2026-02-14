namespace FFmpegWrapper.Containers;


public abstract class IOContext : FFObject<AVIOContext>
{

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected unsafe virtual int ReadPacket(void* opaque, byte* buf, int bufSize)
        => avio_read(Handle, buf, bufSize);

    /// <summary> Forces the internal buffer to be written to the output stream. </summary>
    public void Flush()
    {
        unsafe
        {
            avio_flush(_handle);
        }
    }

    /// <summary> Reads data from the underlying stream to <paramref name="buffer"/>. </summary>
    /// <returns> The number of bytes read. </returns>
    protected abstract int Read(Span<byte> buffer);

    /// <summary> Writes data to the underlying stream. </summary>
    protected abstract void Write(ReadOnlySpan<byte> buffer);

    /// <summary> Sets the position of the underlying stream. </summary>
    protected abstract long Seek(long offset, SeekOrigin origin);

    /// <summary> Returns the number of bytes in the underlying stream. </summary>
    protected virtual long? GetLength() => null;

    protected override unsafe void Free()
    {
        if (_handle != null) {
            fixed (AVIOContext** c = &_handle) avio_closep(c);
        }
    }
}
