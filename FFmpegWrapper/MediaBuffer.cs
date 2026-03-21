namespace FFmpegWrapper;

public readonly struct MediaBuffer<TRaw> : IHandleObserver<AVBufferRef>
    where TRaw : unmanaged
{
    public Handle<AVBufferRef> Handle {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            unsafe {
                return new Handle<AVBufferRef>(_handle, new SkipValidation());
            }
        }
    }

    public Handle<TRaw> Data {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            unsafe {
                return (Handle<TRaw>)Handle.Ref.data;
            }
        }
    }
    
    public NullableHandle UserData {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            unsafe {
                return av_buffer_get_opaque(_handle);
            }
        }
    }

    public int RefCount {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            unsafe {
                return av_buffer_get_ref_count(_handle);
            }
        }
    }
    
    public bool IsWritable {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            unsafe {
                return av_buffer_is_writable(_handle) is 1;
            }
        }
    }

    internal readonly unsafe AVBufferRef* _handle;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public MediaBuffer(Handle<AVBufferRef> handle)
    {
        unsafe {
            _handle = handle;
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public MediaBuffer<TRaw> CreateReference()
    {
        unsafe {
            var reference = av_buffer_ref(_handle);
            
            if (reference is null)
                throw new Exception($"Unable to create reference to {nameof(MediaBuffer<>)}");

            return new MediaBuffer<TRaw>(
                new Handle<AVBufferRef>(reference, new SkipValidation())
            );
        }
    }

    public static explicit operator MediaBuffer<TRaw>(Handle<AVBufferRef> handle) => new(handle);
}
