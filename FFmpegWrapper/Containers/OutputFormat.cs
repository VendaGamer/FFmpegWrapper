namespace FFmpegWrapper.Containers;

using System;

public readonly struct OutputFormat : IFFHandle<AVOutputFormat>, IEquatable<OutputFormat>
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
            Name = Helpers.PtrToStringUTF8(Handle.Ref.name);
            LongName = Helpers.PtrToStringUTF8(Handle.Ref.long_name);
            MimeType = Helpers.PtrToStringUTF8(Handle.Ref.mime_type);
            Extensions = Helpers.PtrToStringUTF8(Handle.Ref.extensions);
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
    
    public readonly ReadOnlySpan<string> GetExtensions()
    {
        
    }

    // Check if this format supports a specific codec
    public unsafe bool SupportsCodec(AVCodecID codecId)
    {
        return ffmpeg.av_guess_codec(Handle, null, null, null, AVMediaType.AVMEDIA_TYPE_UNKNOWN) == codecId;
    }

    // Static methods for common operations
    public static unsafe OutputFormat? GuessFormat(string? shortName = null, string? filename = null, string? mimeType = null)
    {
        var format = ffmpeg.av_guess_format(shortName, filename, mimeType);
        
    }

    // Get all available output formats

    public static ImmutableArray<OutputFormat> AvaliableOutputFormats 
        => Utils.GetAllAvailableOutputFormats();
    
    
    
    /// <summary>
    /// Workaround class.
    /// Cannot be directly in MediaCodec struct cause of this issue:
    /// https://github.com/dotnet/runtime/issues/104511
    /// </summary>
    private static class Utils
    {
        private static ImmutableArray<OutputFormat> AvaliableOutputFormats = default;
        
        public static ImmutableArray<OutputFormat> GetAllAvailableOutputFormats()
        {
            if (!AvaliableOutputFormats.IsDefault) {
                return AvaliableOutputFormats;
            }
            
            var builder = ImmutableArray.CreateBuilder<OutputFormat>(768);
        
            unsafe {
                void* iterState = null;
                AVOutputFormat* outputFormat;
                
                while ((outputFormat = ffmpeg.av_muxer_iterate(&iterState)) != null) {
                    builder.Add(new OutputFormat(outputFormat));
                }
            }

            AvaliableOutputFormats = builder.ToImmutable();
            return AvaliableOutputFormats;
        }
    }

    // Find format by name
    public static bool TryFindByShortName(string shortName, out OutputFormat format)
    {
        if (string.IsNullOrEmpty(shortName))
            return false;
            
        var res = ffmpeg.av_guess_format(shortName, null, null);
        
        format = format is not null ? new OutputFormat(format) : null;
    }
    
    public static unsafe OutputFormat? FindByLongName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return null;
            
        var format = ffmpeg.av_guess_format(name, null, null);
        
        return format is not null ? new OutputFormat(format) : null;
    }

    // Check if format supports specific features
    public unsafe bool SupportsGlobalHeader => (Flags & ffmpeg.AVFMT_GLOBALHEADER) != 0;
    
    public unsafe bool SupportsSeek => (Flags & ffmpeg.AVFMT_SEEK_TO_PTS) != 0;
    
    public unsafe bool RequiresFilename => (Flags & ffmpeg.AVFMT_NOFILE) == 0;

    // Equality implementation
    public unsafe bool Equals(OutputFormat other)
    {
        return Handle == other.Handle;
    }

    public override unsafe bool Equals(object? obj)
    {
        return obj is OutputFormat other && Equals(other);
    }

    public override unsafe int GetHashCode()
    {
        return ((IntPtr)Handle).GetHashCode();
    }

    public static bool operator ==(OutputFormat left, OutputFormat right)
    {
        return left.Equals(right);
    }
    public static bool operator !=(OutputFormat left, OutputFormat right)
    {
        return !left.Equals(right);
    }

    // ToString for debugging
    public override unsafe string ToString()
    {
        if (Handle == null)
            return "OutputFormat(null)";
            
        return $"OutputFormat({Name}: {LongName})";
    }

    // Check if handle is valid
    public bool IsValid {
        get {
            unsafe
            {
                return Handle != null;
            }
        }
    }
}