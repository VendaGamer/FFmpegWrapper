namespace FFmpegWrapper.Containers;

using Core;

public readonly struct InputFormat : IHandleObserver<AVInputFormat>
{
    public Handle<AVInputFormat> Handle {
        get {
            unsafe
            {
                return _handle;
            }
        }
    }

    public AVFormatFlags Flags => (AVFormatFlags)Handle.Ref.flags;

    public readonly string Extensions;
    public readonly string Name;
    public readonly string LongName;
    public readonly string MimeType;

    private readonly unsafe AVInputFormat* _handle;
    
    public InputFormat(Handle<AVInputFormat> handle)
    {
        unsafe {
            _handle = handle;
            Extensions = FFHelper.PtrToStringUtf8(_handle->extensions);
            Name = FFHelper.PtrToStringUtf8(_handle->name);
            LongName = FFHelper.PtrToStringUtf8(_handle->long_name);
            MimeType = FFHelper.PtrToStringUtf8(_handle->mime_type);
        }
    }
}
