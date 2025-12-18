namespace FFmpegWrapper.Media.Formats;

/// <summary>
/// Represents a picture format with dimensions, pixel format, and optional pixel aspect ratio.
/// This is a wrapper around FFmpeg's AVPixelFormat that provides convenient methods for format manipulation.
/// </summary>
/// <remarks>
/// This structure is immutable and provides information about:
/// - Picture dimensions (width and height)
/// - Pixel format (color format and bit depth)
/// - Pixel aspect ratio for non-square pixels
/// - Plane information for planar formats
/// </remarks>
public readonly struct PictureFormat : IEquatable<PictureFormat>
{
    /// <summary>
    /// Gets the width of the picture in pixels.
    /// </summary>
    public readonly int Width;
    
    /// <summary>
    /// Gets the height of the picture in pixels.
    /// </summary>
    public readonly int Height;
    
    /// <summary>
    /// Gets the FFmpeg pixel format that defines the color format and bit depth.
    /// </summary>
    public AVPixelFormat PixelFormat { get; }

    /// <summary>
    /// Gets the pixel aspect ratio as a rational number (width / height).
    /// </summary>
    /// <remarks>
    /// May be 0/1 if unknown or undefined. For square pixels, this should be 1/1.
    /// Non-square pixels are common in some video formats (e.g., anamorphic content).
    /// </remarks>
    public Rational PixelAspectRatio { get; }

    /// <summary>
    /// Gets the number of color planes in this pixel format.
    /// </summary>
    /// <remarks>
    /// Packed formats (like RGB24) have 1 plane, while planar formats (like YUV420P) have multiple planes.
    /// </remarks>
    public int NumPlanes => av_pix_fmt_count_planes(PixelFormat);
    
    /// <summary>
    /// Gets a value indicating whether this pixel format uses a planar layout.
    /// </summary>
    /// <remarks>
    /// Planar formats separate color components into different memory planes (e.g., Y, U, V planes in YUV).
    /// Packed formats interleave color components in a single plane (e.g., RGBRGBRGB...).
    /// </remarks>
    /// <value>Value is result of <see cref="NumPlanes"/> &gt;= 2</value>
    public bool IsPlanar => NumPlanes >= 2;

    /// <summary>
    /// Initializes a new instance of the <see cref="PictureFormat"/> struct with square pixels (1:1 aspect ratio).
    /// </summary>
    /// <param name="width">The width of the picture in pixels.</param>
    /// <param name="height">The height of the picture in pixels.</param>
    /// <param name="pixelFormat">The FFmpeg pixel format.</param>
    public PictureFormat(int width, int height, AVPixelFormat pixelFormat)
    {
        Width = width;
        Height = height;
        PixelFormat = pixelFormat;
        PixelAspectRatio = Rational.Zero;
    }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="PictureFormat"/> struct with a specific pixel aspect ratio.
    /// </summary>
    /// <param name="width">The width of the picture in pixels.</param>
    /// <param name="height">The height of the picture in pixels.</param>
    /// <param name="pixelFormat">The FFmpeg pixel format.</param>
    /// <param name="pixelAspectRatio">The pixel aspect ratio (width/height of individual pixels).</param>
    public PictureFormat(int width, int height, AVPixelFormat pixelFormat, Rational pixelAspectRatio)
    {
        Width = width;
        Height = height;
        PixelFormat = pixelFormat;
        PixelAspectRatio = pixelAspectRatio;
    }

    /// <summary>
    /// Creates a new <see cref="PictureFormat"/> with scaled dimensions.
    /// </summary>
    /// <param name="newWidth">The target width in pixels.</param>
    /// <param name="newHeight">The target height in pixels.</param>
    public PictureFormat GetScaled(int newWidth, int newHeight)
    {
        return new PictureFormat(newWidth, newHeight, PixelFormat, PixelAspectRatio);
    }
    
    /// <summary>
    /// Returns a string representation of this picture format.
    /// </summary>
    /// <returns>A string in the format "WIDTHxHEIGHT PIXEL_FORMAT_NAME".</returns>
    public override string ToString()
    {
        unsafe
        {
            return $"{Width}x{Height} {FFHelper.PtrToStringUtf8(av_get_pix_fmt_name(PixelFormat))}";
        }
    }
    
    /// <summary>
    /// Determines whether the specified <see cref="PictureFormat"/> is equal to this instance.
    /// </summary>
    /// <param name="other">The other picture format to compare.</param>
    /// <returns>True if the formats are equal; otherwise, false.</returns>
    public bool Equals(PictureFormat other) =>
        other.Width == Width && other.Height == Height && 
        other.PixelFormat == PixelFormat &&
        other.PixelAspectRatio.Equals(PixelAspectRatio);

    /// <summary>
    /// Determines whether the specified object is equal to this instance.
    /// </summary>
    /// <param name="obj">The object to compare.</param>
    /// <returns>True if the object is a <see cref="PictureFormat"/> and is equal to this instance; otherwise, false.</returns>
    public override bool Equals(object? obj) => obj is PictureFormat other && Equals(other);
    
    /// <summary>
    /// Returns a hash code for this instance.
    /// </summary>
    /// <returns>A hash code combining width, height, and pixel format.</returns>
    public override int GetHashCode() => (Width, Height, (int)PixelFormat).GetHashCode();
    
    /// <summary>
    /// Determines whether two <see cref="PictureFormat"/> instances are equal.
    /// </summary>
    public static bool operator ==(PictureFormat left, PictureFormat right) => left.Equals(right);

    /// <summary>
    /// Determines whether two <see cref="PictureFormat"/> instances are not equal.
    /// </summary>
    public static bool operator !=(PictureFormat left, PictureFormat right) => !left.Equals(right);
    
}
