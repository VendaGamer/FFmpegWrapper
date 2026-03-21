namespace FFmpegWrapper.Platforms;

public class AndroidMediaCodecContext : FFObject<AVMediaCodecContext>
{
    
    public unsafe AndroidMediaCodecContext()
    {
        NullableHandle<AVMediaCodecContext> allocated = av_mediacodec_alloc_context();

        if (allocated.IsNull)
            throw new Exception($"Could not allocate {nameof(AVMediaCodecContext)}");

        _handle = allocated.Handle;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected override unsafe void Free()
    {
        av_mediacodec_default_free((AVCodecContext*)_handle);
    }
}