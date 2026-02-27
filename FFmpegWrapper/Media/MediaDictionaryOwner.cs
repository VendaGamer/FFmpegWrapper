namespace FFmpegWrapper.Media;

using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

using Extensions;

/// <summary>
/// Efficient wrapper for <see cref="AVDictionary"/>, providing convenient methods for dictionary manipulation.
/// </summary>

[DebuggerDisplay("DecoderConfigs: {decoderConfigs.Count}, EncoderConfigs: {encoderConfigs.Count}")]
public sealed class MediaDictionaryOwner : FFObject<AVDictionary>, IEnumerable<Utf8KeyValue>
    #if NET8_0_OR_GREATER
    ,IUtf8SpanParsable<MediaDictionaryOwner>
    #endif
{
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
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ObservedMediaDictionary ToObserved() => new(this);

    /// <summary>
    /// Gets the number of entries in the dictionary
    /// </summary>
    public int Count {
        get {
            unsafe
            {
                return av_dict_count(Handle);
            }
        }
    }

    /// <summary>
    /// Gets or sets values by key. Throws KeyNotFoundException if key doesn't exist during get.
    /// </summary>
    public ReadOnlySpan<byte> this[ReadOnlySpan<byte> key] 
    {
        get => GetValue(key);
        set => SetValue(key, value);
    }
    
    /// <summary>
    /// Checks if a key exists
    /// </summary>
    public bool ContainsKey(ReadOnlySpan<byte> key)
    {
        unsafe
        {
            return av_dict_get(Handle, key.RawHandle, null, 0) != null;
        }
    }

    /// <summary>
    /// Gets the value associated with the given key, or null if there is no match.
    /// </summary>
    public ReadOnlySpan<byte> GetValue(ReadOnlySpan<byte> key, AVDictFlags flags = AVDictFlags.AV_DICT_MATCH_CASE)
    {
        unsafe
        {
            var entry = av_dict_get(Handle, key.RawHandle, null, (int)flags);
            return entry is null ? default : FFHelper.GetSpanFromSentinelTerminatedPtr<byte>(entry->value, 0);
        }
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long SetIntValue(ReadOnlySpan<byte> key, long value,
        AVDictFlags flags = AVDictFlags.AV_DICT_MATCH_CASE)
    {
        unsafe
        {
            fixed(AVDictionary** ptr = &_handle)
                av_dict_set_int(ptr, key.RawHandle, value, (int)flags).CheckError();
            return value;
        }
    }

    /// <summary>
    /// Tries to get a value, returning false if the key doesn't exist
    /// </summary>
    public bool TryGetValue(ReadOnlySpan<byte> key, out ReadOnlySpan<byte> value, AVDictFlags flags = 0)
    {
        value = GetValue(key, flags);
        return value.IsEmpty;
    }

    /// <summary>
    /// Sets the value associated with the given key, overwriting it if necessary.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetValue(ReadOnlySpan<byte> key, ReadOnlySpan<byte> value,
        AVDictFlags flags = AVDictFlags.AV_DICT_MATCH_CASE |
                            AVDictFlags.AV_DICT_DONT_STRDUP_VAL |
                            AVDictFlags.AV_DICT_DONT_STRDUP_KEY)
    {
        unsafe
        {
            fixed (AVDictionary** ptr = &_handle) {
                av_dict_set(ptr, key.RawHandle, value.RawHandle, (int)flags).CheckError();
            }
        }
    }

    /// <summary>
    /// Clears all entries from the dictionary
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear()
    {
        unsafe
        {
            fixed (AVDictionary** ptr = &_handle) {
                av_dict_free(ptr);
                av_dict_copy(ptr, null, 0);
            }
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
        parsed = new MediaDictionaryOwner(handle);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe MediaDictionaryOwner CreateCopyOf(Handle<AVDictionary> source)
    {
        AVDictionary* handle = null;
        av_dict_copy(&handle ,source,0).CheckError("Unable to ");
        return new MediaDictionaryOwner(handle);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MediaDictionaryOwner CreateFromEntries(params ReadOnlySpan<Utf8KeyValue> entries)
        => CreateFromEntries(entries, 0);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe MediaDictionaryOwner CreateFromEntries(ReadOnlySpan<Utf8KeyValue> entries, AVDictFlags flags)
    {
        AVDictionary* handle = null;
        
        foreach (var entry in entries) {
            av_dict_set(&handle, entry.Key.RawHandle, entry.Value.RawHandle, 0).CheckError();
        }
        
        return new MediaDictionaryOwner(handle);
    }

    /// <summary>
    /// Copies entries from another dictionary
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CopyFrom(Handle<AVDictionary> other, AVDictFlags flags = 0)
    {
        unsafe
        {
            fixed (AVDictionary** ptr = &_handle) {
                av_dict_copy(ptr, other, (int)flags).CheckError();
            }
        }
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
            if (_handle != null) {
                ReadOnlySpan<byte> a;
                fixed (AVDictionary** handle = &_handle) {
                    av_dict_free(handle);
                }
            }
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe Enumerator GetEnumerator() => new(Handle);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    IEnumerator<Utf8KeyValue> IEnumerable<Utf8KeyValue>.GetEnumerator() => GetEnumerator();
    
    public struct Enumerator : IEnumerator<Utf8KeyValue>
    {
        private readonly unsafe AVDictionary* _dict;
        private unsafe AVDictionaryEntry* _currentEntry;
        
        internal unsafe Enumerator(AVDictionary* dict)
        {
            _dict = dict;
            _currentEntry = null;
        }
        
        /// <summary>
        /// Current entry
        /// </summary>
        public unsafe Utf8KeyValue Current => *_currentEntry;

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            unsafe {
                _currentEntry = av_dict_iterate(_dict, _currentEntry);
                return _currentEntry is not null;
            }
        }
        object IEnumerator.Current => Current;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void IEnumerator.Reset()
        {
            unsafe
            {
                _currentEntry = null;
            }
        }
        
        void IDisposable.Dispose()
        {
            
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MediaDictionaryOwner Parse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider)
    {
        if (!TryParse(utf8Text, provider, out var parsed)) {
            throw new ArgumentException("Could not parse from", nameof(utf8Text));
        }

        return parsed;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryParse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider, [MaybeNullWhen(false)] out MediaDictionaryOwner result)
    {
        return TryParseFromUtf8String(utf8Text, ":"u8, "|"u8, out result);
    }
}