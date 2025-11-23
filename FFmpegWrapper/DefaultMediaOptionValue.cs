namespace FFmpegWrapper;

public readonly struct DefaultMediaOptionValue : IFFHandleObserver<AVOption_default_val>
{
    public FFHandle<AVOption_default_val> Handle {
        get {
            unsafe {
                return _handle;
            }
        }
    }

    public double DoubleValue => Handle.Ref.dbl;
    public long LongValue => Handle.Ref.i64;
    public Rational RationalValue => Handle.Ref.q;
    public readonly string StringValue;
    


    internal readonly unsafe AVOption_default_val* _handle;

    public DefaultMediaOptionValue(FFHandle<AVOption_default_val> handle)
    {
        unsafe
        {
            StringValue = FFHelper.PtrToStringUtf8(_handle->str);
        }
    }
}