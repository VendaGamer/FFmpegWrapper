using CommunityToolkit.HighPerformance;
using CommunityToolkit.HighPerformance.Enumerables;

namespace FFmpegWrapper.Containers;

using Extensions;

public readonly struct OutputFormat : IFFHandleObserver<AVOutputFormat>
{
    public FFHandle<AVOutputFormat> Handle {
        get {
            unsafe
            {
                return _handle;
            }
        }
    }

    private readonly unsafe AVOutputFormat* _handle;

    // Private constructor to ensure only valid instances are created
    public OutputFormat(FFHandle<AVOutputFormat> handle)
    {
        unsafe
        {
            _handle = handle;
            Name = FFHelper.PtrToStringUtf8(Handle.Ref.name);
            LongName = FFHelper.PtrToStringUtf8(Handle.Ref.long_name);
            MimeType = FFHelper.PtrToStringUtf8(Handle.Ref.mime_type);
            Extensions = FFHelper.PtrToStringUtf8(Handle.Ref.extensions);
        }
    }

    // Properties wrapping AVOutputFormat fields
    
    
    public readonly string Name;
    public readonly string LongName;
    public readonly string MimeType;
    
    /// <summary>
    /// Comma seperated extensions
    /// </summary>
    public readonly string Extensions;
    
    public AVCodecID AudioCodec => Handle.Ref.audio_codec;
    
    public AVCodecID VideoCodec => Handle.Ref.video_codec;
    
    public AVCodecID SubtitleCodec => Handle.Ref.subtitle_codec;
    
    public int Flags => Handle.Ref.flags;

    /// <summary>
    /// Useful for iterating over <see cref="OutputFormat"/> extensions
    /// </summary>
    public readonly ReadOnlySpanTokenizer<char> GetExtensionsTokenizer()
        => Extensions.Tokenize(',');

    // Check if this format supports a specific codec
    public unsafe bool SupportsCodec(AVCodecID codecId)
    {
        return av_guess_codec(Handle, null, null, null, AVMediaType.AVMEDIA_TYPE_UNKNOWN) == codecId;
    }

    // Get all available output formats

    public static ImmutableArray<OutputFormat> AvailableOutputFormats 
        => Utils.GetAllAvailableOutputFormats();
    
    
    
    /// <summary>
    /// Workaround class.
    /// Cannot be directly in MediaCodec struct cause of this issue:
    /// https://github.com/dotnet/runtime/issues/104511
    /// </summary>
    private static class Utils
    {
        private static ImmutableArray<OutputFormat> s_availableOutputFormats = default;
        
        public static ImmutableArray<OutputFormat> GetAllAvailableOutputFormats()
        {
            if (!s_availableOutputFormats.IsDefault) {
                return s_availableOutputFormats;
            }
            
            var builder = ImmutableArray.CreateBuilder<OutputFormat>(768);
        
            unsafe {
                void* iterState = null;
                AVOutputFormat* outputFormat;
                
                while ((outputFormat = av_muxer_iterate(&iterState)) != null) {
                    builder.Add(new OutputFormat(outputFormat));
                }
            }

            s_availableOutputFormats = builder.ToImmutable();
            return s_availableOutputFormats;
        }
    }

    // Find format by name
    public static bool TryFindByShortName(ReadOnlySpan<byte> shortName, out OutputFormat format)
    {
        unsafe {

            if (shortName.IsEmpty)
                goto NotFound;

            
            var res = av_guess_format(shortName.RawHandle, null, null);

            if (res is not null) {
                format = new OutputFormat(res);
                return true;
            }
            
            NotFound:
            format = default!;
            return false;
        }
    }
    
    // Find format by name
    public static bool TryFindByFileName(ReadOnlySpan<byte> fileName, out OutputFormat format)
    {
        unsafe {

            if (fileName.IsEmpty)
                goto NotFound;

            
            var res = av_guess_format(null, fileName.RawHandle, null);

            if (res is not null) {
                format = new OutputFormat(res);
                return true;
            }
            
            NotFound:
            format = default!;
            return false;
        }
    }
    
    public static bool TryFindByMimeType(ReadOnlySpan<byte> fileName, out OutputFormat format)
    {
        unsafe {

            if (fileName.IsEmpty)
                goto NotFound;

            
            var res = av_guess_format(null, null, fileName.RawHandle);

            if (res is not null) {
                format = new OutputFormat(res);
                return true;
            }
            
            NotFound:
            format = default!;
            return false;
        }
    }

    private const byte DOT = (byte)'.';
    
    public static bool TryFindByExtension(ReadOnlySpan<byte> extension, out OutputFormat outputFormat)
    {
        unsafe {

            if (extension.IsEmpty)
                goto NotFound;
            
            var res = av_guess_format(null, extension.RawHandle, null);

            if (res is not null) {
                outputFormat = new OutputFormat(res);
                return true;
            }
            
            NotFound:
            outputFormat = default;
            return false;
        }
    }

    public static OutputFormat FindByExtenion(ReadOnlySpan<char> extension)
    {
        if (TryFindByExtension(extension, out OutputFormat format)) {
            return format;
        }

        throw new ArgumentException(nameof(extension));
    }

    public static bool TryFindByExtension(
        ReadOnlySpan<char> extension,
        out OutputFormat outputFormat,
        StringComparison comparisonType = StringComparison.Ordinal)
    {
        var formats = AvailableOutputFormats.AsSpan();
        
        for (int i = 0; i < formats.Length; i++) {
            foreach (var span in formats[i].GetExtensionsTokenizer()) {
                if (span.Equals(extension, comparisonType)) {
                    outputFormat = formats[i];
                    return true;
                }
            }
        }
        
        
        outputFormat = default!;
        return false;
    }
    

    // Check if format supports specific features
    
    public bool SupportsGlobalHeader => (Flags & (int)AVFormatCapabilityFlags.AVFMT_GLOBALHEADER) != 0;
    
    public bool SupportsSeek => (Flags & (int)AVFormatCapabilityFlags.AVFMT_SEEK_TO_PTS) != 0;
    
    public bool RequiresFilename => (Flags & (int)AVFormatCapabilityFlags.AVFMT_NOFILE) == 0;
    

    public override unsafe bool Equals(object? obj)
    {
        return obj is OutputFormat other && Equals(other);
    }

    public override int GetHashCode() => Handle.GetHashCode();

    public static bool operator ==(OutputFormat left, OutputFormat right)
    {
        return left.Equals(right);
    }
    public static bool operator !=(OutputFormat left, OutputFormat right)
    {
        return !left.Equals(right);
    }

    // ToString for debugging
    public override string ToString()
    {
        unsafe
        {
            if (_handle is null)
                return "OutputFormat(null)";
            
            return $"OutputFormat({Name}: {LongName})";
        }
    }
}
