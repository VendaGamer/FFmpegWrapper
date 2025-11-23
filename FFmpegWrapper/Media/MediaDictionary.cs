namespace FFmpegWrapper.Media;

using System.Collections;
using Abstractions;
using Core.Flags;

/// <summary>
/// Efficient wrapper for AVDictionary with minimal overhead operations
/// </summary>
public sealed class MediaDictionary : FFObject<AVDictionary>, IEnumerable<MediaDictionaryEntry>
{
    /// <summary>
    /// Creates a new owned MediaDictionary
    /// </summary>
    public MediaDictionary()
    {
        unsafe {
            fixed (AVDictionary** ptr = &_handle) {
                ffmpeg.av_dict_copy(ptr ,null,0);
            }
        }
    }

    /// <summary>
    /// Wraps an existing AVDictionary pointer (takes ownership of the pointer)
    /// </summary>
    public MediaDictionary(FFHandle<AVDictionary> target)
    {
        unsafe
        {
            _handle = target;
        }
    }

    /// <summary>
    /// Gets the number of entries in the dictionary
    /// </summary>
    public int Count {
        get {
            unsafe
            {
                return ffmpeg.av_dict_count(Handle);
            }
        }
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
        unsafe
        {
            return ffmpeg.av_dict_get(Handle, key, null, 0) != null;
        }
    }

    /// <summary>
    /// Gets the value associated with the given key, or null if there is no match.
    /// </summary>
    public string? GetValue(string key, DictionaryFlags flags = DictionaryFlags.MatchCase)
    {
        unsafe
        {
            var entry = ffmpeg.av_dict_get(Handle, key, null, (int)flags);
            return entry == null ? null : FFHelper.PtrToStringUtf8(entry->value);
        }
    }

    /// <summary>
    /// Tries to get a value, returning false if the key doesn't exist
    /// </summary>
    public bool TryGetValue(string key, out string? value, DictionaryFlags flags = DictionaryFlags.None)
    {
        value = GetValue(key, flags);
        return value != null;
    }

    /// <summary>
    /// Sets the value associated with the given key, overwriting it if necessary.
    /// </summary>
    public void SetValue(string key, string? value, DictionaryFlags flags = DictionaryFlags.None)
    {
        unsafe
        {
            fixed (AVDictionary** ptr = &_handle) {
                ffmpeg.av_dict_set(ptr, key, value, (int)flags).CheckError();
            }
        
            base._handle = _handle;
        }
    }

    /// <summary>
    /// Removes a key from the dictionary
    /// </summary>
    public bool Remove(string key, DictionaryFlags flags = DictionaryFlags.None)
    {
        bool existed = ContainsKey(key);
        if (existed)
        {
            unsafe
            {
                fixed(AVDictionary** ptr = &_handle)
                {
                    ffmpeg.av_dict_set(ptr, key, null, (int)flags).CheckError();
                }
                base._handle = _handle;
            }
        }
        return existed;
    }

    /// <summary>
    /// Clears all entries from the dictionary
    /// </summary>
    public void Clear()
    {
        unsafe
        {
            fixed (AVDictionary** ptr = &_handle) {
                ffmpeg.av_dict(ptr);
            }
            base._handle = _handle; // Should be null after free
        }
    }

    /// <summary>
    /// Efficiently populates a dictionary from key-value pairs
    /// </summary>
    public static void Populate(FFHandleSource<AVDictionary> dict, IEnumerable<Entry>? options)
    {
        if (options == null) return;

        foreach (var entry in options) {
            unsafe
            {
                ffmpeg.av_dict_set(dict, entry.Key, entry.Value, 0).CheckError();
            }
        }
    }

    /// <summary>
    /// Copies entries from another dictionary
    /// </summary>
    public void CopyFrom(MediaDictionary other, DictionaryFlags flags = 0)
    {
        foreach (var VARIABLE in GetEnumerator()) {
            
        }
        unsafe
        {
            fixed (AVDictionary** prt = &_handle) {
                ffmpeg.av_dict_copy(prt, other.Handle, (int)flags).CheckError();
            }
        }
    }
    
    public Enumerator GetEnumerator()
    {
        unsafe
        {
            return new Enumerator(Handle);
        }
    }

    IEnumerator<MediaDictionaryEntry> IEnumerable<MediaDictionaryEntry>.GetEnumerator()
        => GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public override string ToString()
    {
        return ToString((byte)':', (byte)'|');
    }

    public string ToString(byte utf8KeyValueSeparatorChar, byte utf8PairsSeparator)
    {
        unsafe
        {
            byte* buffer = null!;
            ffmpeg.av_dict_get_string(_handle, &buffer, utf8KeyValueSeparatorChar, utf8PairsSeparator).CheckError();
        
            string str = FFHelper.PtrToStringUtf8(buffer);
            ffmpeg.av_free(buffer);
        
            return str;
        }
    }

    protected override void Free()
    {
        unsafe
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
    }
    
    public ref struct Enumerator : IEnumerator<MediaDictionaryEntry>
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
        public MediaDictionaryEntry Current
        {
            get {
                unsafe
                {
                    return new MediaDictionaryEntry(_currentEntry);
                }
            }
        }

        /// <inheritdoc />
        public bool MoveNext()
        {
            unsafe {
                _currentEntry = ffmpeg.av_dict_iterate(_dict, _currentEntry);
                return _currentEntry is not null;
            }
        }
        object IEnumerator.Current => Current;

        void IEnumerator.Reset()
        {
            unsafe
            {
                _currentEntry = null;
            }
        }
        void IDisposable.Dispose(){ }
    }
}