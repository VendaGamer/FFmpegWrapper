namespace FFmpegWrapper.Containers;

using System;

using CommunityToolkit.HighPerformance;
using CommunityToolkit.HighPerformance.Enumerables;

public readonly struct OutputFormat : IFFHandleObserver<AVOutputFormat>, IEquatable<OutputFormat>
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
            Name = FFHelper.PtrToStringUTF8(Handle.Ref.name);
            LongName = FFHelper.PtrToStringUTF8(Handle.Ref.long_name);
            MimeType = FFHelper.PtrToStringUTF8(Handle.Ref.mime_type);
            Extensions = FFHelper.PtrToStringUTF8(Handle.Ref.extensions);
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
        return ffmpeg.av_guess_codec(Handle, null, null, null, AVMediaType.AVMEDIA_TYPE_UNKNOWN) == codecId;
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
                
                while ((outputFormat = ffmpeg.av_muxer_iterate(&iterState)) != null) {
                    builder.Add(new OutputFormat(outputFormat));
                }
            }

            s_availableOutputFormats = builder.ToImmutable();
            return s_availableOutputFormats;
        }
    }

    // Find format by name
    public static bool TryFindByShortName(string shortName, out OutputFormat format)
    {
        unsafe {

            if (string.IsNullOrEmpty(shortName))
                goto NotFound;

            
            var res = ffmpeg.av_guess_format(shortName, null, null);

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
    public static bool TryFindByFileName(string fileName, out OutputFormat format)
    {
        unsafe {

            if (string.IsNullOrEmpty(fileName))
                goto NotFound;

            
            var res = ffmpeg.av_guess_format(null, fileName, null);

            if (res is not null) {
                format = new OutputFormat(res);
                return true;
            }
            
            NotFound:
            format = default!;
            return false;
        }
    }
    
    public static bool TryFindByMimeType(string fileName, out OutputFormat format)
    {
        unsafe {

            if (string.IsNullOrEmpty(fileName))
                goto NotFound;

            
            var res = ffmpeg.av_guess_format(null, null, fileName);

            if (res is not null) {
                format = new OutputFormat(res);
                return true;
            }
            
            NotFound:
            format = default!;
            return false;
        }
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