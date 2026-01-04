namespace FFmpegWrapper.Containers;

using System.Runtime.InteropServices;

public readonly struct MediaIOContext : IFFHandleObserver<AVIOContext>
{
    #region Properties

    public FFHandle<AVIOContext> Handle {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            unsafe {
                return _handle;
            }
        }
    }
    
    public ReadOnlySpan<byte> Buffer {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            unsafe {
                var handle = Handle.Raw;
                return new ReadOnlySpan<byte>(handle->buffer, handle->buffer_size);
            }
        }
    }


    public long BytesWritten {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Handle.Ref.bytes_written;
    }
    
    public long BytesRead {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Handle.Ref.bytes_read;
    }
    
    public nuint Checksum {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Handle.Ref.checksum;
    }
    
    public bool IsWrite {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Handle.Ref.write_flag is 1;
    }

    public bool ReachedEndOfFile {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Handle.Ref.write_flag is 1;
    }

    #endregion
    
    
    internal readonly unsafe AVIOContext* _handle;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public MediaIOContext(FFHandle<AVIOContext> handle)
    {
        unsafe {
            _handle = handle;
        }
    }
}