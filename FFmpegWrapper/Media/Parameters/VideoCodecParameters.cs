namespace FFmpegWrapper.Media.Parameters;

public class VideoCodecParameters : MediaCodecParameters
{
    
    #region Properties

    public PictureColorspace Colorspace {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            ref var handle = ref Handle.Ref;
            
            return new PictureColorspace(
                handle.color_space,
                handle.color_primaries,
                handle.color_trc,
                handle.color_range,
                handle.chroma_location);
        }
    }
    
    /// <inheritdoc cref="AVCodecParameters.framerate"/>
    public Rational FrameRate => Handle.Ref.framerate;
    
    /// <inheritdoc cref="AVCodecParameters.video_delay" />
    public int VideoDelay => Handle.Ref.video_delay;
    
    public AVFieldOrder FieldOrder => Handle.Ref.field_order;
    
    public PictureFormat PictureFormat {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            ref var handle = ref Handle.Ref;
            
            return new PictureFormat(
                handle.width,
                handle.height,
                (AVPixelFormat)handle.format,
                handle.sample_aspect_ratio);
        }
    }
    
    #endregion
    
}