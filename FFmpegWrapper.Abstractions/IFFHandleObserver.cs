namespace FFmpegWrapper.Abstractions;

using Core;

/// <summary>
/// Represents unmanaged type of
/// TODO: comment
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IFFHandleObserver<T>
    where T : unmanaged
{
    FFHandle<T> Handle { get; }
}