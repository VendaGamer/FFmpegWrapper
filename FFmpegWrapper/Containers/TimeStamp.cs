namespace FFmpegWrapper.Containers;

public readonly struct TimeStamp
{
    public const long InvalidValue = long.MinValue;
    

    public readonly long Value;

    public bool IsValid => Value is not InvalidValue;

    public TimeStamp()
    {
        Value = InvalidValue;
    }

    private TimeStamp(long pts)
    {
        
    }

    private static TimeStamp FromRational(Rational rational)
    {
        return new TimeStamp();
    }

    public static TimeStamp FromSeconds()
    {
        //TODO:
        return default;
    }
}