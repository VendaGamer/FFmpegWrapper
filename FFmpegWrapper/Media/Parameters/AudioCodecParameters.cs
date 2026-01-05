namespace FFmpegWrapper.Media.Parameters;

public class AudioCodecParameters : MediaCodecParameters
{
    /// <inheritdoc cref="AVCodecParameters.block_align" />
    public int BlockAlign {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Handle.Ref.block_align;
    }

    /// <inheritdoc cref="AVCodecParameters.frame_size" />
    public int FrameSize {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Handle.Ref.frame_size;
    }
    
    /// <inheritdoc cref="AVCodecParameters.initial_padding" />
    public int InitialPaddingSamples {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Handle.Ref.initial_padding;
    }

    /// <inheritdoc cref="AVCodecParameters.trailing_padding" />
    public int TrailingPaddingSamples {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Handle.Ref.trailing_padding;
    }

    /// <inheritdoc cref="AVCodecParameters.seek_preroll" />
    public int SeekPreroll {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Handle.Ref.seek_preroll;
    }
    
    public AudioFormat AudioFormat {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            ref var handle = ref Handle.Ref;
            
            return new AudioFormat(
                (AVSampleFormat)handle.format,
                handle.sample_rate,
                handle.ch_layout
            );
        }
    }
}