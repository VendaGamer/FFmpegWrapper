namespace FFmpegWrapper.Core;

/// <summary>
/// Represents unmanaged type of
/// TODO: comment
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IFFHandle<T>
    where T : unmanaged
{
    FFHandle<T> Handle { get; }
}