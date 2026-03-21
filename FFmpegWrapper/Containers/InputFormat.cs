namespace FFmpegWrapper.Containers;

using Core;

public readonly struct InputFormat : IHandleObserver<AVInputFormat>
{
    public Handle<AVInputFormat> Handle {
        get {
            unsafe
            {
                return (Handle<AVInputFormat>)_handle;
            }
        }
    }

    public AVFormatFlags Flags => (AVFormatFlags)Handle.Ref.flags;

    public ReadOnlySpan<byte> Extensions {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            unsafe {
                return FFHelper.Utf8SpanFromPtrNullTerm(Handle.Ref.extensions);
            }
        }
    }

    public ReadOnlySpan<byte> Name {
        get {
            unsafe {
                return FFHelper.Utf8SpanFromPtrNullTerm(Handle.Ref.name);
            }
        }
    }

    public ReadOnlySpan<byte> LongName {
        get {
            unsafe {
                return FFHelper.Utf8SpanFromPtrNullTerm(Handle.Ref.long_name);
            }
        }
    }

    public ReadOnlySpan<byte> MimeType {
        get {
            unsafe {
                return FFHelper.Utf8SpanFromPtrNullTerm(Handle.Ref.mime_type);
            }
        }
    }

    private readonly unsafe AVInputFormat* _handle;
    
    public InputFormat(Handle<AVInputFormat> handle)
    {
        unsafe {
            _handle = handle;
        }
    }
}
