namespace FFmpegWrapper;

public readonly struct OptionValue
{
    private readonly object _value;

    public object BoxedValue => _value;

    private OptionValue(object obj)
    {
        _value = obj;
        
        Type = obj switch {
            string => AVOptionType.AV_OPT_TYPE_STRING,
            bool => AVOptionType.AV_OPT_TYPE_BOOL,
            AVDictionary => AVOptionType.AV_OPT_TYPE_DICT,
            long => AVOptionType.AV_OPT_TYPE_INT,
            double => AVOptionType.AV_OPT_TYPE_DOUBLE,
            AVRational => AVOptionType.AV_OPT_TYPE_RATIONAL,
            AVChannelLayout => AVOptionType.AV_OPT_TYPE_CHLAYOUT,
            AVPixelFormat => AVOptionType.AV_OPT_TYPE_PIXEL_FMT,
            AVSampleFormat => AVOptionType.AV_OPT_TYPE_SAMPLE_FMT,
            byte[] => AVOptionType.AV_OPT_TYPE_BINARY,
            uint4 => AVOptionType.AV_OPT_TYPE_COLOR,
            uint => AVOptionType.AV_OPT_TYPE_FLAGS,
            
        };
        
    }

    private OptionValue(object obj, AVOptionType optionType)
    {
        _value = obj;
        Type = optionType;
    }

    // public static OptionValue From<T>(FFHandle<T> t)
    //     where T : unmanaged
    // {
    //     
    // }
    //
    // public static OptionValue From<T>(T t)
    //     where T : Enum
    // {
    //     
    // }

    public readonly AVOptionType Type;

    public string AsString() => (string)_value;
    public long AsInteger() => (long)_value;
    public double AsDouble() => (double)_value;
    public Rational AsRational() => (Rational)_value;
    public ChannelLayout AsChannelLayout() => (ChannelLayout)_value;
    public AVPixelFormat AsPixelFormat() => (AVPixelFormat)_value;
    public AVSampleFormat AsSampleFormat() => (AVSampleFormat)_value;
    public byte[] AsBinary() => (byte[])_value;

    public override string? ToString() => _value.ToString();

    public static implicit operator OptionValue(string val) => new(val);
    public static implicit operator OptionValue(long val) => new(val);
    public static implicit operator OptionValue(double val) => new(val);
    public static implicit operator OptionValue(Rational val) => new(val);
    public static implicit operator OptionValue(ChannelLayout val) => new(val);
    public static implicit operator OptionValue(AVPixelFormat val) => new(val);
    public static implicit operator OptionValue(AVSampleFormat val) => new(val);
    public static implicit operator OptionValue(byte[] val) => new(val);
}