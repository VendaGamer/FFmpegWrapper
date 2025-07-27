namespace FFmpegWrapper.Containers;

using System;
using System.Runtime.InteropServices;

public readonly struct OutputFormat : IHandle<AVOutputFormat>, IEquatable<OutputFormat>
{
    public unsafe AVOutputFormat* Handle { get; } = null!;

    // Private constructor to ensure only valid instances are created
    private unsafe OutputFormat(AVOutputFormat* handle)
    {
        Handle = handle;
    }

    // Factory method to create from handle
    public static unsafe OutputFormat FromHandle(AVOutputFormat* handle)
    {
        if (handle == null)
            throw new ArgumentNullException(nameof(handle));
        
        return new OutputFormat(handle);
    }

    // Properties wrapping AVOutputFormat fields
    public unsafe string Name => Marshal.PtrToStringAnsi((IntPtr)Handle->name) ?? string.Empty;
    
    public unsafe string LongName => Marshal.PtrToStringAnsi((IntPtr)Handle->long_name) ?? string.Empty;
    
    public unsafe string MimeType => Marshal.PtrToStringAnsi((IntPtr)Handle->mime_type) ?? string.Empty;
    
    public unsafe string Extensions => Marshal.PtrToStringAnsi((IntPtr)Handle->extensions) ?? string.Empty;
    
    public unsafe AVCodecID AudioCodec => Handle->audio_codec;
    
    public unsafe AVCodecID VideoCodec => Handle->video_codec;
    
    public unsafe AVCodecID SubtitleCodec => Handle->subtitle_codec;
    
    public unsafe int Flags => Handle->flags;

    // Check if this format supports a specific codec
    public unsafe bool SupportsCodec(AVCodecID codecId)
    {
        return ffmpeg.av_guess_codec(Handle, null, null, null, AVMediaType.AVMEDIA_TYPE_UNKNOWN) == codecId;
    }

    // Static methods for common operations
    public static unsafe OutputFormat? GuessFormat(string? shortName = null, string? filename = null, string? mimeType = null)
    {
        var format = ffmpeg.av_guess_format(shortName, filename, mimeType);
        return format != null ? FromHandle(format) : null;
    }

    // Get all available output formats

    private static ImmutableArray<OutputFormat> allOutputFormats;

    public static ImmutableArray<OutputFormat> AllOutputFormats {
        get {
            if (!allOutputFormats.IsDefault) {
                return allOutputFormats;
            }

            return allOutputFormats = IterateOverFormats();
        }
    }
    
    private static unsafe ImmutableArray<OutputFormat> IterateOverFormats()
    {
        var formats = ImmutableArray.CreateBuilder<OutputFormat>();
        void* opaque = null;
        AVOutputFormat* format;
        
        while ((format = ffmpeg.av_muxer_iterate(&opaque)) != null)
        {
            formats.Add(FromHandle(format));
        }
        
        return formats.ToImmutable();
    }

    // Find format by name
    public static unsafe OutputFormat? FindByName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return null;
            
        var format = ffmpeg.av_guess_format(name, null, null);
        return format != null ? FromHandle(format) : null;
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