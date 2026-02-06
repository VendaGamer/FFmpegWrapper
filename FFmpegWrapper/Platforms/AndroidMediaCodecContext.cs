namespace FFmpegWrapper.Platforms;

public class AndroidMediaCodecContext : OwnedObject<AVMediaCodecContext>
{
    
    public unsafe AndroidMediaCodecContext() : base(av_mediacodec_alloc_context())
    {
        MediaCodecBuffer a;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected override unsafe void Free()
    {
        av_mediacodec_default_free((AVCodecContext*)_handle);
    }
}