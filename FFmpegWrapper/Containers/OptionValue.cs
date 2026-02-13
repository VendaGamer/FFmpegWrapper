namespace FFmpegWrapper.Containers;

public readonly struct OptionValue : IHandleObserver<AVOption_default_val>
{
    public Handle<AVOption_default_val> Handle {
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

    public long Long {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Handle.Ref.i64;
    }

    public double Double {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Handle.Ref.dbl;
    }

    public Rational Rational => Handle.Ref.q;
    
    internal readonly unsafe AVOption_default_val* _handle;
    public readonly AVOptionType ValueType;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public OptionValue(Handle<AVOption_default_val> handle, AVOptionType valueType)
    {
        unsafe {
            _handle = handle;
            ValueType = valueType;
        }
    }
}