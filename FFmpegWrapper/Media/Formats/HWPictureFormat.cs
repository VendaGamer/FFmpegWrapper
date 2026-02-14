namespace FFmpegWrapper.Media.Formats;

public readonly struct HWPictureFormat : IEquatable<HWPictureFormat>
{
    public readonly int Width;
    public readonly int Height;
    public readonly AVPixelFormat SWFormat;
    public readonly AVPixelFormat HWFormat;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HWPictureFormat(AVPixelFormat hwFormat, AVPixelFormat swFormat, int width, int height)
    {
        Width = width;
        Height = height;
        SWFormat = swFormat;
        HWFormat = hwFormat;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(HWPictureFormat other)
    {
        return Width == other.Width && Height == other.Height;
    }
}