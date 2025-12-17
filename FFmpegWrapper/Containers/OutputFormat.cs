using CommunityToolkit.HighPerformance;
using CommunityToolkit.HighPerformance.Enumerables;

namespace FFmpegWrapper.Containers;

using System.Buffers.Text;

using Extensions;

public readonly struct OutputFormat : IFFHandleObserver<AVOutputFormat>
{
    public FFHandle<AVOutputFormat> Handle {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
        }
    }

    // Properties wrapping AVOutputFormat fields
    
    
    public ReadOnlySpan<byte> Name {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            unsafe
            {
                return FFHelper.Utf8SpanFromPtrNullTerm(Handle.Ref.name);
            }
        }
    }

    public ReadOnlySpan<byte> LongName {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            unsafe {
                return FFHelper.Utf8SpanFromPtrNullTerm(Handle.Ref.long_name);
            }
        }
    }

    public ReadOnlySpan<byte> MimeType {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            unsafe {
                return FFHelper.Utf8SpanFromPtrNullTerm(Handle.Ref.mime_type);
            }
        }
    }

    /// <summary>
    /// Comma seperated extensions
    /// </summary>
    public ReadOnlySpan<byte> Extensions {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            unsafe {
                return FFHelper.Utf8SpanFromPtrNullTerm(Handle.Ref.extensions);
            }
        }
    }
    
    public AVCodecID AudioCodec => Handle.Ref.audio_codec;
    
    public AVCodecID VideoCodec => Handle.Ref.video_codec;
    
    public AVCodecID SubtitleCodec => Handle.Ref.subtitle_codec;
    
    public int Flags => Handle.Ref.flags;

    /// <summary>
    /// Useful for iterating over <see cref="OutputFormat"/> extensions
    /// </summary>
    public readonly ReadOnlySpanTokenizer<byte> GetExtensionsTokenizer()
        => Extensions.Tokenize((byte)',');

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
    public static bool TryFindByShortName(scoped ReadOnlySpan<byte> shortName, out OutputFormat format)
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
    public static bool TryFindByFileName(scoped ReadOnlySpan<byte> fileName, out OutputFormat format)
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
    
    public static bool TryFindByMimeType(scoped ReadOnlySpan<byte> fileName, out OutputFormat format)
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
    
    
    public static bool TryFindByExtension(scoped ReadOnlySpan<byte> extension, out OutputFormat outputFormat)
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OutputFormat FindByExtension(scoped ReadOnlySpan<byte> extension)
    {
        if (TryFindByExtension(extension, out OutputFormat format))
            return format;

        throw new ArgumentException("No output format with such extension", nameof(extension));
    }

    public override bool Equals(object? obj)
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
}
