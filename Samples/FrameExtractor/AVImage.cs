using FFmpegBindings.Abstractions;

public class AVImage(string filePath, AVPixelFormat pixelFormat)
{
    public readonly string FilePath = filePath;
    public readonly AVPixelFormat PixelFormat = pixelFormat;
}