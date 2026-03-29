namespace FFmpegWrapper.Media;

using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Extensions;

/// <summary>
/// Efficient wrapper for <see cref="AVDictionary"/>, providing convenient methods for dictionary manipulation.
/// </summary>

[DebuggerDisplay("{ToString}")]
public sealed class MediaDictionaryOwner : FFObject<AVDictionary>
    #if NET8_0_OR_GREATER
    , IUtf8SpanParsable<MediaDictionaryOwner>
    #endif
{
    public MediaDictionary<MediaDictionaryOwner> Dictionary {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            ThrowIfDisposed();
            
            return new MediaDictionary<MediaDictionaryOwner>(this);
        }
    }
    
    /// <summary>
    /// Creates a new owned MediaDictionary
    /// </summary>
    public MediaDictionaryOwner()
    {
        unsafe {
            fixed (AVDictionary** ptr = &_handle) {
                av_dict_set(ptr,null , null,0);
            }
        }
    }

    /// <summary>
    /// Wraps an existing AVDictionary pointer (takes ownership of the pointer)
    /// </summary>
    public MediaDictionaryOwner(Handle<AVDictionary> target)
    {
        unsafe
        {
            _handle = target;
        }
    }

    public static unsafe bool TryParseFromUtf8String(
        ReadOnlySpan<byte> factoryString, 
        ReadOnlySpan<byte> keyValueSeparator,
        ReadOnlySpan<byte> pairSeparator, 
        [NotNullWhen(true)] out MediaDictionaryOwner? parsed,
        AVDictFlags flags = 0,
        bool allowNotFullyParsed = false
    ) {
        AVDictionary* handle = null;
        var res = av_dict_parse_string(&handle, factoryString.RawHandle,
            keyValueSeparator.RawHandle, pairSeparator.RawHandle,
            (int)flags);

        if (res < 0) {
            if (handle is null) {
                parsed = null;
                return false;
            }

            if (allowNotFullyParsed) {
                goto Succeded;
            }
            
            av_dict_free(&handle);
            parsed = null;
            return false;
        }
        
        Succeded:
        parsed = new MediaDictionaryOwner(WrapperHelper.UnsafeHandle(handle));
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe MediaDictionaryOwner CreateCopyOf(Handle<AVDictionary> source)
    {
        AVDictionary* handle = null;
        av_dict_copy(&handle ,source,0).CheckError("Unable to ");
        return new MediaDictionaryOwner((Handle<AVDictionary>)handle);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryCreateFromEntries(
        [NotNullWhen(true)]
        out MediaDictionaryOwner? owner,
        params ReadOnlySpan<Utf8KeyValue> entries
    ) => TryCreateFromEntries(entries, out owner,0);
    
    private static unsafe bool TryCreateFromEntries(
        ReadOnlySpan<Utf8KeyValue> entries,
        [NotNullWhen(true)]
        out MediaDictionaryOwner? owner,
        AVDictFlags flags)
    {
        if (entries.IsEmpty)
            goto Fail;
        
        AVDictionary* handle = null;
        
        foreach (var entry in entries) {
            av_dict_set(&handle, entry.Key.RawHandle, entry.Value.RawHandle, 0);
        }

        if (handle is null)
            goto Fail;
        
        owner = new MediaDictionaryOwner(WrapperHelper.UnsafeHandle(handle));
        return true;
        
        Fail:
        owner = null;
        return false;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override string ToString() => ToString((byte)':', (byte)'|');

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ToString(byte utf8KeyValueSeparatorChar, byte utf8PairsSeparator)
    {
        unsafe
        {
            byte* buffer = null!;
            av_dict_get_string(_handle, &buffer, utf8KeyValueSeparatorChar, utf8PairsSeparator).CheckError();
        
            string str = FFHelper.PtrToStringUtf8(buffer);
            av_free(buffer);
        
            return str;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected override void Free()
    {
        unsafe
        {
            if(_handle is null)
                return;
            
            fixed (AVDictionary** handle = &_handle) {
                av_dict_free(handle);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MediaDictionaryOwner Parse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider = null)
    {
        if (!TryParse(utf8Text, provider, out var parsed)) {
            throw new ArgumentException("Could not parse from", nameof(utf8Text));
        }

        return parsed;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryParse(
        ReadOnlySpan<byte> utf8Text,
        IFormatProvider? provider,
        [MaybeNullWhen(false)]
        out MediaDictionaryOwner result)
    {
        return TryParseFromUtf8String(utf8Text, ":"u8, "|"u8, out result);
    }
}