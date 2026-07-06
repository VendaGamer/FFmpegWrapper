namespace FFmpegWrapper.Core;

using System.Diagnostics;
using System.Text;

public sealed class FFmpegException : Exception
{
    public FFmpegException(AVError error, string message) : base(message)
    {
        
    }
    
    public FFmpegException(AVError error) : base(GetErrorString((int)error))
    {
        
    }

    private static unsafe string GetErrorString(int error)
    {
        var buffer = stackalloc byte[1024];
        var result = (AVError)FFmpeg.av_strerror(error, buffer, 1024);
        Debug.Assert(result is 0);
        
        return Encoding.UTF8.GetString(buffer, FFHelper.Strlen(buffer));
    }
}