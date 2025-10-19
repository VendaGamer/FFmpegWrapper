namespace FFmpegWrapper.Abstractions;

public interface IFFWrapped<out T>
    where T : unmanaged
{
    T Native { get; }
}