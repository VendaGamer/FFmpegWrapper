namespace FFmpegWrapper.Abstractions;

using Core;

public interface IFFHandleOwner<T> : IDisposable
    where T : unmanaged
{
    public FFHandle<T> Handle { get; }
}