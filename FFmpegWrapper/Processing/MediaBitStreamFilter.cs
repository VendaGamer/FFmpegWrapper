namespace FFmpegWrapper.Processing;

using Media;

public readonly struct MediaBitStreamFilter : IHandleObserver<AVBitStreamFilter>
{
    public static ImmutableArray<MediaBitStreamFilter> AvailableFilters {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Utils.GetAllAvailableOutputFormats();
    }
    
    /// <summary>
    /// Workaround class.
    /// Cannot be directly in MediaCodec struct cause of this issue:
    /// https://github.com/dotnet/runtime/issues/104511
    /// </summary>
    private static class Utils
    {
        private static ImmutableArray<MediaBitStreamFilter> s_availableFilters;
        
        public static ImmutableArray<MediaBitStreamFilter> GetAllAvailableOutputFormats()
        {
            if (!s_availableFilters.IsDefault) {
                return s_availableFilters;
            }
            
            var builder = ImmutableArray.CreateBuilder<MediaBitStreamFilter>(64);
        
            unsafe {
                void* iterState = null;
                AVBitStreamFilter* filter;
                
                while ((filter = av_bsf_iterate(&iterState)) is not null) {
                    builder.Add(*(MediaBitStreamFilter*)&filter);
                }
            }

            s_availableFilters = builder.ToImmutable();
            return s_availableFilters;
        }
    }
    
    public unsafe Handle<AVBitStreamFilter> Handle {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(_handle);
    }

    public unsafe ReadOnlySpan<AVCodecID> SupportedCodecIds {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => FFHelper.GetSpanFromSentinelTerminatedPtr(Handle.Ref.codec_ids, AVCodecID.AV_CODEC_ID_NONE);
    }

    public unsafe ReadOnlySpan<byte> Name {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => FFHelper.Utf8SpanFromPtrNullTerm(_handle->name);
    }

    public unsafe MediaClass Class {
        get => new(WrapperHelper.UnsafeHandle(Handle.Ref.priv_class));
    }

    public static unsafe bool TryGetByName(
        ReadOnlySpan<byte> name,
        out MediaBitStreamFilter filter)
    {
        fixed (byte* ptr = name)
        {
            var handle = av_bsf_get_by_name(ptr);
            if (handle is null) {
                filter = default;
                return false;
            }

            filter = new MediaBitStreamFilter(WrapperHelper.UnsafeHandle(handle));
            return true;
        }
    }

    private unsafe readonly AVBitStreamFilter* _handle;

    public MediaBitStreamFilter(Handle<AVBitStreamFilter> handle)
    {
        unsafe
        {
            _handle = handle;
        }
    }
}