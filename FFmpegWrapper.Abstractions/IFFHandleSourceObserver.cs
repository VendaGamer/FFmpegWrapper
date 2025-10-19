namespace FFmpegWrapper.Abstractions;

using Core;

public interface IFFHandleSourceObserver<T>
    where T : unmanaged
{
    FFHandleSource<T> Native { get; }
}