namespace FFmpegWrapper.Abstractions;

using Core;

/// <summary>
/// Represents wrapper used for directly modifying structures owned by ffmpeg's objects.
/// </summary>
/// <typeparam name="T">Observed handle type</typeparam>
public interface IFFHandleObserver<T>
    where T : unmanaged
{
    FFHandle<T> Handle { get; }
}