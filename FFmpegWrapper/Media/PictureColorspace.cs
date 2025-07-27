namespace FFmpegWrapper.Media;


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