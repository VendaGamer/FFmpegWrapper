using FFmpegWrapper.Media.Formats;

namespace FrameExtractor;

public record AVImage(string FilePath, PictureFormat PixelFormat);