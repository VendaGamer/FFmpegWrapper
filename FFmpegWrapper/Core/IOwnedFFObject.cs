namespace FFmpegWrapper.Core;

using System.Diagnostics;

public interface IOwnedFFObject<T> where T : unmanaged
{
    T OwnedObject { get; }
    
    
}