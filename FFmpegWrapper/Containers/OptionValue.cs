namespace FFmpegWrapper.Containers;

public readonly struct OptionValue : IFFHandleObserver<AVOption_u>
{
    public FFHandle<AVOption_u> Handle {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            unsafe {
                return _handle;
            }
        }
    }

    public ReadOnlySpan<byte> String {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            unsafe {
                return FFHelper.Utf8SpanFromPtrNullTerm(Handle.Ref.str);
            }
        }
    }

    public long Long => Handle.Ref.i64;
    public double Double => Handle.Ref.dbl;
    public Rational Rational => Handle.Ref.q;
    
    internal readonly unsafe AVOption_u* _handle;
    public readonly AVOptionType ValueType;

    public OptionValue(FFHandle<AVOption_u> handle, AVOptionType valueType)
    {
        unsafe {
            _handle = handle;
            ValueType = valueType;
        }
    }
}