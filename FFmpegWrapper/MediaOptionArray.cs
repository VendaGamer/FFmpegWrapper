namespace FFmpegWrapper;

public readonly struct MediaOptionArray : IHandleObserver<AVOptionArrayDef>
{
    public Handle<AVOptionArrayDef> Handle {
        get {
            unsafe {
                return _handle;
            }
        }
    }

    public ReadOnlySpan<byte> Buffer {
        get {
            unsafe
            {
                return FFHelper.GetSpanFromSentinelTerminatedPtr<byte>(Handle.Ref.def, 0);
            }
        }
    }

    public uint MaxSize => Handle.Ref.size_max;

    public uint MinSize => Handle.Ref.size_min;

    public byte Separator => Handle.Ref.sep;

    internal readonly unsafe AVOptionArrayDef* _handle;

    public MediaOptionArray(Handle<AVOptionArrayDef> handle)
    {
        unsafe {
            _handle = handle;
        }
    }
}
