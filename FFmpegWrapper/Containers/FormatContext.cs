namespace FFmpegWrapper.Containers;

using Abstractions;

using Extensions;

using Media;

public abstract class FormatContext(FFHandle<AVFormatContext> handle) : FFObject<AVFormatContext>(handle)
{
    public long FileSize {
        get {
            unsafe {
                return avio_size(Handle.Ref.pb);
            }
        }
    }
    
    public uint StreamCount => Handle.Ref.nb_streams;
    
    
    
    public long BitRate {
        get => Handle.Ref.bit_rate;
        set {
            unsafe {
                ThrowIfDisposed();
                
                if (_handle->duration > 0 &&  avio_size(_handle->pb) > 0) {
                    throw new InvalidOperationException("Do not set bitrate if duration and filesize is known");
                }
                
                _handle->bit_rate = value;
            }
        }
    }

    public MediaDictionary metadata {
        get {
            unsafe
            {
                return new MediaDictionary(Handle.Ref.metadata);
            }
        }
    }
    
    private unsafe FormatContext(AVOutputFormat* outputFormat, ReadOnlySpan<byte> formatName, ReadOnlySpan<byte> filename)
    {
        fixed (AVFormatContext** ptr = &_handle) {
            avformat_alloc_output_context2(ptr, outputFormat, formatName.RawHandle, filename.RawHandle);
        }
    }

    protected FormatContext()
    {
        unsafe {
            _handle = ;
        }
    }
    
    /// <inheritdoc/>
    protected override unsafe void Free()
    {
        avformat_free_context(_handle);
    }
}
