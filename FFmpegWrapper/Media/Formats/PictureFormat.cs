namespace FFmpeg.Wrapper;
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
    public int NumPlanes => ffmpeg.av_pix_fmt_count_planes(PixelFormat);
    
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
    /// <param name="newFormat">The new pixel format, or <see cref="PixelFormats.None"/> to keep the current format.</param>
    /// <param name="keepAspectRatio">If true, maintains the original aspect ratio by scaling proportionally.</param>
    /// <param name="align">Ensures that width and height are multiples of this value (useful for codec requirements).</param>
    /// <returns>A new <see cref="PictureFormat"/> with the specified scaling applied.</returns>
    /// <remarks>
    /// When <paramref name="keepAspectRatio"/> is true, the smaller of the two scale factors is used to ensure
    /// the entire original image fits within the target dimensions. The alignment parameter is useful for
    /// codecs that require dimensions to be multiples of specific values (e.g., 16 for some H.264 configurations).
    /// </remarks>
    public PictureFormat GetScaled(int newWidth, int newHeight, AVPixelFormat newFormat = PixelFormats.None, bool keepAspectRatio = true, int align = 1)
    {
        if (keepAspectRatio) {
            double scale = Math.Min(newWidth / (double)Width, newHeight / (double)Height);
            newWidth = (int)Math.Round(Width * scale);
            newHeight = (int)Math.Round(Height * scale);
        }
        if (newFormat == PixelFormats.None) {
            newFormat = PixelFormat;
        }
        if (align > 1) {
            newWidth = (newWidth + align - 1) / align * align;
            newHeight = (newHeight + align - 1) / align * align;
        }
        return new PictureFormat(newWidth, newHeight, newFormat);
    }
    
    /// <summary>
    /// Returns a string representation of this picture format.
    /// </summary>
    /// <returns>A string in the format "WIDTHxHEIGHT PIXEL_FORMAT_NAME".</returns>
    public override string ToString()
    {
        return $"{Width}x{Height} {ffmpeg.av_get_pix_fmt_name(PixelFormat)}";
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

/// <summary>
/// Contains colorspace information for video frames, including matrix coefficients, color primaries, 
/// transfer characteristics, and color range.
/// </summary>
/// <remarks>
/// Color space handling in video is complex and affects how colors are represented and displayed.
/// This structure wraps FFmpeg's color space parameters. For more information, see:
/// https://trac.ffmpeg.org/wiki/colorspace
/// 
/// Key concepts:
/// - Matrix: Defines the conversion between RGB and YUV color spaces (e.g., BT.709, BT.2020)
/// - Primaries: Defines the color gamut (e.g., BT.709 for HD, BT.2020 for UHD)
/// - Transfer: Defines the gamma/EOTF curve (e.g., BT.709, SMPTE 2084 for HDR)
/// - Range: Defines whether values use full range (0-255) or limited range (16-235 for 8-bit)
/// </remarks>
public readonly struct PictureColorspace : IEquatable<PictureColorspace>
{

    /// <summary>
    /// Gets the color matrix coefficients that define YUV↔RGB conversion.
    /// </summary>
    /// <remarks>
    /// Common values include BT.709 (HD), BT.601 (SD), and BT.2020 (UHD).
    /// </remarks>
    public readonly AVColorSpace Matrix;

    /// <summary>
    /// Gets the color primaries that define the color gamut.
    /// </summary>
    /// <remarks>
    /// Defines the chromaticity coordinates of the red, green, and blue primaries.
    /// Common values include BT.709 (HD), BT.2020 (UHD), and DCI-P3 (cinema).
    /// </remarks>
    public readonly AVColorPrimaries Primaries;
    
    /// <summary>
    /// Gets the transfer characteristics (gamma curve/EOTF).
    /// </summary>
    /// <remarks>
    /// Defines how electrical signal values map to light output.
    /// Common values include BT.709 (standard gamma), SMPTE 2084 (PQ for HDR), and HLG (hybrid log-gamma for HDR).
    /// </remarks>
    public readonly AVColorTransferCharacteristic Transfer;
    
    /// <summary>
    /// Gets the color range (full or limited).
    /// </summary>
    /// <remarks>
    /// Limited range uses values 16-235 for luma and 16-240 for chroma in 8-bit.
    /// Full range uses the complete 0-255 range in 8-bit.
    /// </remarks>
    public readonly AVColorRange Range;
    
    /// <summary>
    /// Gets a value indicating whether this colorspace represents HDR content.
    /// </summary>
    /// <remarks>
    /// HDR is identified by the use of HDR transfer characteristics like PQ (SMPTE 2084) or HLG.
    /// </remarks>
    public bool IsHDR => Transfer is AVColorTransferCharacteristic.AVCOL_TRC_SMPTE2084 
                                    or AVColorTransferCharacteristic.AVCOL_TRC_ARIB_STD_B67;
    
    /// <summary>
    /// Gets a value indicating whether this colorspace uses wide color gamut.
    /// </summary>
    /// <remarks>
    /// Wide color gamut is typically associated with BT.2020 primaries or DCI-P3.
    /// </remarks>
    public bool IsWideGamut => Primaries is AVColorPrimaries.AVCOL_PRI_BT2020
                                            or AVColorPrimaries.AVCOL_PRI_SMPTE432;
    
    /// <summary>
    /// Gets a value indicating whether this colorspace is suitable for standard HD content.
    /// </summary>
    public bool IsStandardHD => Matrix == AVColorSpace.AVCOL_SPC_BT709 && 
                                Primaries == AVColorPrimaries.AVCOL_PRI_BT709 && 
                                Transfer == AVColorTransferCharacteristic.AVCOL_TRC_BT709;
    
    /// <summary>
    /// Gets a value indicating whether this colorspace uses full range values.
    /// </summary>
    public bool IsFullRange => Range == AVColorRange.AVCOL_RANGE_JPEG;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="PictureColorspace"/> struct.
    /// </summary>
    /// <param name="matrix">The color matrix coefficients.</param>
    /// <param name="primaries">The color primaries.</param>
    /// <param name="trc">The transfer characteristics.</param>
    /// <param name="range">The color range.</param>
    public PictureColorspace(AVColorSpace matrix, AVColorPrimaries primaries, AVColorTransferCharacteristic trc, AVColorRange range)
    {
        Matrix = matrix;
        Primaries = primaries;
        Transfer = trc;
        Range = range;
    }
    
    /// <summary>
    /// Creates a colorspace configuration for standard HD content (BT.709).
    /// </summary>
    /// <param name="fullRange">Whether to use full range (0-255) or limited range (16-235).</param>
    /// <returns>A <see cref="PictureColorspace"/> configured for HD content.</returns>
    public static PictureColorspace CreateHD(bool fullRange = false)
    {
        return new PictureColorspace(
            AVColorSpace.AVCOL_SPC_BT709,
            AVColorPrimaries.AVCOL_PRI_BT709,
            AVColorTransferCharacteristic.AVCOL_TRC_BT709,
            fullRange ? AVColorRange.AVCOL_RANGE_JPEG : AVColorRange.AVCOL_RANGE_MPEG
        );
    }
    
    /// <summary>
    /// Creates a colorspace configuration for UHD HDR content (BT.2020 + PQ).
    /// </summary>
    /// <param name="fullRange">Whether to use full range or limited range.</param>
    /// <returns>A <see cref="PictureColorspace"/> configured for UHD HDR content.</returns>
    public static PictureColorspace CreateUHD_HDR_PQ(bool fullRange = false)
    {
        return new PictureColorspace(
            AVColorSpace.AVCOL_SPC_BT2020_NCL,
            AVColorPrimaries.AVCOL_PRI_BT2020,
            AVColorTransferCharacteristic.AVCOL_TRC_SMPTE2084, // PQ
            fullRange ? AVColorRange.AVCOL_RANGE_JPEG : AVColorRange.AVCOL_RANGE_MPEG
        );
    }
    
    /// <summary>
    /// Creates a colorspace configuration for UHD HDR content (BT.2020 + HLG).
    /// </summary>
    /// <param name="fullRange">Whether to use full range or limited range.</param>
    /// <returns>A <see cref="PictureColorspace"/> configured for UHD HDR content with HLG.</returns>
    public static PictureColorspace CreateUHD_HDR_HLG(bool fullRange = false)
    {
        return new PictureColorspace(
            AVColorSpace.AVCOL_SPC_BT2020_NCL,
            AVColorPrimaries.AVCOL_PRI_BT2020,
            AVColorTransferCharacteristic.AVCOL_TRC_ARIB_STD_B67, // HLG
            fullRange ? AVColorRange.AVCOL_RANGE_JPEG : AVColorRange.AVCOL_RANGE_MPEG
        );
    }
    
    /// <summary>
    /// Creates a colorspace configuration for standard definition content (BT.601).
    /// </summary>
    /// <param name="fullRange">Whether to use full range or limited range.</param>
    /// <returns>A <see cref="PictureColorspace"/> configured for SD content.</returns>
    public static PictureColorspace CreateSD(bool fullRange = false)
    {
        return new PictureColorspace(
            AVColorSpace.AVCOL_SPC_BT470BG, // or SMPTE170M for NTSC
            AVColorPrimaries.AVCOL_PRI_BT470BG,
            AVColorTransferCharacteristic.AVCOL_TRC_BT709, // Often BT.709 gamma is used even for SD
            fullRange ? AVColorRange.AVCOL_RANGE_JPEG : AVColorRange.AVCOL_RANGE_MPEG
        );
    }
    
    /// <summary>
    /// Checks if this colorspace is compatible with another for direct conversion.
    /// </summary>
    /// <param name="other">The other colorspace to check compatibility with.</param>
    /// <returns>True if the colorspaces are compatible; otherwise, false.</returns>
    /// <remarks>
    /// Compatible colorspaces can be converted without significant quality loss.
    /// Different primaries or transfer characteristics may require more complex conversions.
    /// </remarks>
    public bool IsCompatibleWith(PictureColorspace other)
    {
        // Same colorspace is always compatible
        if (Equals(other)) return true;

        // Same primaries and transfer, different range is easily convertible
        if (Primaries == other.Primaries && Transfer == other.Transfer && Matrix == other.Matrix)
            return true;

        // BT.709 and BT.601 with same transfer are reasonably compatible
        if ((Matrix == AVColorSpace.AVCOL_SPC_BT709 && other.Matrix == AVColorSpace.AVCOL_SPC_BT470BG) ||
            (Matrix == AVColorSpace.AVCOL_SPC_BT470BG && other.Matrix == AVColorSpace.AVCOL_SPC_BT709))
        {
            return Transfer == other.Transfer && Primaries == other.Primaries;
        }

        return false;
    }
    
    /// <summary>
    /// Gets a description of the colorspace complexity for debugging/logging purposes.
    /// </summary>
    /// <returns>A string describing the colorspace characteristics.</returns>
    public string GetComplexityDescription()
    {
        var parts = new List<string>();
        
        if (IsHDR) parts.Add("HDR");
        if (IsWideGamut) parts.Add("Wide Gamut");
        if (IsFullRange) parts.Add("Full Range");
        
        if (Transfer == AVColorTransferCharacteristic.AVCOL_TRC_SMPTE2084) parts.Add("PQ");
        else if (Transfer == AVColorTransferCharacteristic.AVCOL_TRC_ARIB_STD_B67) parts.Add("HLG");
        
        return parts.Count > 0 ? string.Join(", ", parts) : "Standard";
    }
    
    /// <summary>
    /// Returns a detailed string representation of this colorspace.
    /// </summary>
    /// <returns>A string describing the range, matrix, primaries, and transfer characteristics.</returns>
    public override string ToString()
    {
        return $"{ffmpeg.av_color_range_name(Range)}, {ffmpeg.av_color_space_name(Matrix)}/{ffmpeg.av_color_primaries_name(Primaries)}/{ffmpeg.av_color_transfer_name(Transfer)}";
    }
    
    /// <summary>
    /// Determines whether the specified <see cref="PictureColorspace"/> is equal to this instance.
    /// </summary>
    /// <param name="other">The other colorspace to compare.</param>
    /// <returns>True if the colorspaces are equal; otherwise, false.</returns>
    public bool Equals(PictureColorspace other) =>
        Matrix == other.Matrix && Primaries == other.Primaries && 
        Transfer == other.Transfer && Range == other.Range;

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is PictureColorspace other && Equals(other);
    }
    
    /// <summary>
    /// Returns a hash code for this instance.
    /// </summary>
    /// <returns>A hash code combining all colorspace parameters.</returns>
    public override int GetHashCode() => ((int)Matrix, (int)Primaries, (int)Transfer, (int)Range).GetHashCode();
    
    /// <summary>
    /// Determines whether two <see cref="PictureColorspace"/> instances are equal.
    /// </summary>
    public static bool operator ==(PictureColorspace left, PictureColorspace right) => left.Equals(right);
    
    /// <summary>
    /// Determines whether two <see cref="PictureColorspace"/> instances are not equal.
    /// </summary>
    public static bool operator !=(PictureColorspace left, PictureColorspace right) => !left.Equals(right);
    
}