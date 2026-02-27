namespace FFmpegWrapper.Containers;

using FFmpegBindings.Abstractions;

using Core;

public readonly struct MediaDuration
{
    public long? Duration => _duration is 0 ? null : _duration;
    public TimeSpan? DurationTimeSpan => Rational.GetTimeSpan(_duration, TimeBase);


    private readonly long _duration;
    public readonly Rational TimeBase;
    
    public MediaDuration(long duration) : this(duration, FFmpegConstants.AV_TIME_BASE_Q)
    {
        
    }
    
    public MediaDuration(long duration, Rational timeBase)
    {
        _duration = duration;
        TimeBase = timeBase;
    }
}