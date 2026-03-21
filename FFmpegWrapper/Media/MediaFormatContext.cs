namespace FFmpegWrapper.Media;

public class MediaFormatContext : FFObject<AVFormatContext>
{
    public static MediaClass Class {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            unsafe {
                return new MediaClass(
                    new Handle<AVClass>(
                        avformat_get_class(),
                        new SkipValidation()
                    )
                );
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