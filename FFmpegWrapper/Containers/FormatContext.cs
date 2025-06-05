namespace FFmpeg.Wrapper;

public abstract class FormatContext : FFObject<AVFormatContext>
{
    public long FileSize {
        get {
            unsafe {
                return ffmpeg.avio_size(Handle->pb);
            }
        }
    }
    
    public uint StreamCount
    {
        get
        {
            unsafe
            {
                return Handle->nb_streams;
            }
        }
    }
    
    
    
    public long BitRate {
        get {
            unsafe {
                return Handle->bit_rate;
            }
        }
        set {
            unsafe {
                ThrowIfDisposed();
                
                if (_handle->duration > 0 &&  ffmpeg.avio_size(_handle->pb) > 0) {
                    throw new InvalidOperationException("Do not set bitrate if duration and filesize is known");
                }
                
                _handle->bit_rate = value;
            }
        }
    }

    public unsafe MediaDictionary metadata => new(&_handle->metadata);

    protected unsafe FormatContext(AVOutputFormat* outputFormat) : this(outputFormat,null,null)
    {
        
    }

    private unsafe FormatContext(AVOutputFormat* outputFormat, string? formatName, string? filename)
    {
        fixed (AVFormatContext** ptr = &_handle) {
            ffmpeg.avformat_alloc_output_context2(ptr, outputFormat, formatName, filename);
        }
    }

    protected FormatContext()
    {
        unsafe {
            _handle = ffmpeg.avformat_alloc_context();
        }
    }
    
    /// <inheritdoc/>
    protected override unsafe void Free()
    {
        ffmpeg.avformat_free_context(_handle);
    }
}