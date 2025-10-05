namespace FFmpegWrapper.Media;

using System.Collections;

using CommunityToolkit.HighPerformance.Buffers;

using Flags;

using Entry = KeyValuePair<string, string>;

/// <summary> Wrapper for an existing <see cref="AVDictionaryEntry"/>. </summary>

public readonly ref struct MediaDictionaryEntry : IHandle<AVDictionaryEntry>
{
    private readonly unsafe AVDictionaryEntry* _handle;
    unsafe AVDictionaryEntry* IHandle<AVDictionaryEntry>.Handle => _handle;

    public string Key {
        get {
            unsafe
            {
                return Helpers.PtrToStringUTF8(_handle->key);
            }
        }
    }

    public string Value {
        get {
            unsafe {
                return Helpers.PtrToStringUTF8(_handle->value);
            }
        }
    }

    public Entry ToEntry() => new(Key, Value);
    public unsafe MediaDictionaryEntry(AVDictionaryEntry* handle)
    {
        this._handle = handle;
    }
}
/// <summary>
/// Efficient wrapper for AVDictionary with minimal overhead operations
/// </summary>
public sealed unsafe class MediaDictionary : FFObject<AVDictionary>, IEnumerable<Entry>
{
    private readonly bool _ownsTarget;

    /// <summary>
    /// Creates a new owned MediaDictionary
    /// </summary>
    public MediaDictionary()
    {
        // Allocate a pointer that we own
        _handle = (AVDictionary*)ffmpeg.av_mallocz((ulong)sizeof(AVDictionary));
        if (_handle == null) 
            throw new OutOfMemoryException("Failed to allocate AVDictionary pointer");
        
        base._handle = _handle; // handle is initially null
        _ownsTarget = true;
    }

    /// <summary>
    /// Wraps an existing AVDictionary pointer (does not take ownership of the pointer itself)
    /// </summary>
    public MediaDictionary(FFHandle<AVDictionary> target)
    {
        base._handle = target;
        _ownsTarget = false;
    }

    /// <summary>
    /// Gets the number of entries in the dictionary
    /// </summary>
    public int Count 
    {
        get => ffmpeg.av_dict_count(Handle);
    }

    /// <summary>
    /// Gets or sets values by key. Throws KeyNotFoundException if key doesn't exist during get.
    /// </summary>
    public string this[string key] 
    {
        get => GetValue(key) ?? throw new KeyNotFoundException($"Key '{key}' not found in dictionary");
        set => SetValue(key, value);
    }

    /// <summary>
    /// Checks if a key exists
    /// </summary>
    public bool ContainsKey(string key)
    {
        return ffmpeg.av_dict_get(Handle, key, null, 0) != null;
    }

    /// <summary>
    /// Gets the value associated with the given key, or null if there is no match.
    /// </summary>
    public string? GetValue(string key, bool matchCase = false, bool matchPrefix = false)
    {
        int flags = 0;
        if (matchCase) flags |= ffmpeg.AV_DICT_MATCH_CASE;
        if (matchPrefix) flags |= ffmpeg.AV_DICT_IGNORE_SUFFIX;

        var entry = ffmpeg.av_dict_get(Handle, key, null, flags);
        return entry == null ? null : Helpers.PtrToStringUTF8(entry->value);
    }

    /// <summary>
    /// Tries to get a value, returning false if the key doesn't exist
    /// </summary>
    public bool TryGetValue(string key, out string? value, bool matchCase = false, bool matchPrefix = false)
    {
        value = GetValue(key, matchCase, matchPrefix);
        return value != null;
    }

    /// <summary>
    /// Sets the value associated with the given key, overwriting it if necessary.
    /// </summary>
    public void SetValue(string key, string? value, bool allowMultiple = false)
    {
        int flags = allowMultiple ? ffmpeg.AV_DICT_MULTIKEY : 0;
        
        // Update our handle after the operation since av_dict_set can reallocate
        fixed (AVDictionary** ptr = &_handle) {
            ffmpeg.av_dict_set(ptr, key, value, flags).CheckError();
        }
        
        base._handle = _handle;
    }

    /// <summary>
    /// Removes a key from the dictionary
    /// </summary>
    public bool Remove(string key)
    {
        bool existed = ContainsKey(key);
        if (existed)
        {
            fixed(AVDictionary** ptr = &_handle)
            {
                ffmpeg.av_dict_set(ptr, key, null, 0).CheckError();
            }
            base._handle = _handle;
        }
        return existed;
    }

    /// <summary>
    /// Clears all entries from the dictionary
    /// </summary>
    public void Clear()
    {
        fixed (AVDictionary** ptr = &_handle) {
            ffmpeg.av_dict_free(ptr);
        }
        base._handle = _handle; // Should be null after free
    }

    /// <summary>
    /// Efficiently populates a dictionary from key-value pairs
    /// </summary>
    public static void Populate(AVDictionary** dict, IEnumerable<Entry>? options)
    {
        if (options == null) return;

        foreach (var entry in options) 
        {
            ffmpeg.av_dict_set(dict, entry.Key, entry.Value, 0).CheckError();
        }
    }

    /// <summary>
    /// Copies entries from another dictionary
    /// </summary>
    public void CopyFrom(MediaDictionary other, DictionaryFlags flags = 0)
    {
        fixed (AVDictionary** prt = &_handle) {
            ffmpeg.av_dict_copy(prt, other.Handle, (int)flags).CheckError();
        }
    }

    /// <summary>
    /// Gets a fast enumerator that avoids boxing
    /// </summary>
    public Enumerator GetEnumerator() => new(Handle);
    
    IEnumerator<Entry> IEnumerable<Entry>.GetEnumerator() => GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public override string ToString()
    {
        return ToString((byte)':', (byte)'|');
    }

    public string ToString(byte utf8KeyValueSeparatorChar, byte utf8PairsSeparator)
    {
        byte* buffer = null!;

        ffmpeg.av_dict_get_string(_handle, &buffer, utf8KeyValueSeparatorChar, utf8PairsSeparator).CheckError();
        
        string str = Helpers.PtrToStringUTF8(buffer);
        ffmpeg.av_free(buffer);
        
        return str;
    }

    protected override void Free()
    {
        if (_handle != null)
        {
            fixed (AVDictionary** handle = &_handle) {
                ffmpeg.av_dict_free(handle);
            }
            if (_ownsTarget)
            {
                ffmpeg.av_free(_handle);
            }
        }
    }
    
    public struct Enumerator : IEnumerator<Entry>
    {
        private readonly AVDictionary* _dict;
        private AVDictionaryEntry* _entry;
        
        internal Enumerator(AVDictionary* dict)
        {
            _dict = dict;
            _entry = null;
        }
        
        /// <summary>
        /// Current entry
        /// </summary>
        public Entry Current 
        {
            get 
            {
                // Direct string creation without null checks for performance
                // av_dict_iterate guarantees non-null key/value
                return new Entry(
                    Helpers.PtrToStringUTF8(_entry->key),
                    Helpers.PtrToStringUTF8(_entry->value)
                );
            }
        }

        /// <summary>
        /// Gets current entry as MediaDictionaryEntry
        /// </summary>
        public MediaDictionaryEntry CurrentEntry 
        {
            get => new(_entry);
        }

        /// <inheritdoc />
        public bool MoveNext()
        {
            _entry = ffmpeg.av_dict_iterate(_dict, _entry);
            return _entry is not null;
        }
        object IEnumerator.Current => Current;
        void IEnumerator.Reset() => throw new NotSupportedException("Reset is not supported on dictionary enumerators");
        void IDisposable.Dispose(){ }
    }
}