namespace FFmpegWrapper.Media.Parameters;

public class VideoCodecParameters : MediaCodecParameters
{
    
    #region Properties
    
    /// <inheritdoc cref="AVCodecParameters.framerate"/>
    public Rational FrameRate => Handle.Ref.framerate;
    
    public PictureFormat PictureFormat {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            ref var handle = ref Handle.Ref;
            return new PictureFormat(handle.width, handle.height, (AVPixelFormat)handle.format, handle.sample_aspect_ratio);
        }
    }
    
    #endregion
    
}