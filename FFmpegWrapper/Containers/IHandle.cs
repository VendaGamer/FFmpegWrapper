namespace FFmpeg.Wrapper;

public interface IHandle<T> where T : unmanaged
{
    unsafe T* Handle { get; }
}