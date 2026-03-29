namespace FFmpegWrapper.Media;

public class MediaFormatContext : FFObject<AVFormatContext>
{
    public static MediaClass Class {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            unsafe {
                var mediaClass = avformat_get_class();
                return *(MediaClass*)mediaClass;
            }
        }
    }

    public static ReadOnlySpan<byte> BuildTimeConfiguration {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            unsafe
            {
                return FFHelper.Utf8SpanFromPtrNullTerm(avformat_configuration());
            }
        }
    }
    
    public MediaFormatContext()
    {
        unsafe {
            _handle = avformat_alloc_context();

            if (_handle is null)
                throw new Exception($"Could not allocate {nameof(AVFormatContext)}");
        }
    }
    
    protected override unsafe void Free()
    {
        avformat_free_context(_handle);
    }
}