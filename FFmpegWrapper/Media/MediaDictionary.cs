namespace FFmpegWrapper.Media;

using System.Collections;
using Abstractions;

using CommunityToolkit.HighPerformance;

using Extensions;

/// <summary>
/// Efficient wrapper for AVDictionary with minimal overhead operations
/// </summary>
public sealed class MediaDictionary : FFObject<AVDictionary>
{
    /// <summary>
    /// Creates a new owned MediaDictionary
    /// </summary>
    public MediaDictionary()
    {
        unsafe {
            fixed (AVDictionary** ptr = &_handle) {
                av_dict_copy(ptr ,null,0);
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
    public void SetValue(ReadOnlySpan<byte> key, ReadOnlySpan<byte> value, AVDictFlags flags = 0)
    {
        unsafe
        {
            fixed (AVDictionary** ptr = &_handle) {
                av_dict_set(ptr, key.RawHandle, value.RawHandle, (int)flags).CheckError();
                av_dict_copy(ptr, null, (int)flags).CheckError();
            }
        }
    }

    /// <summary>
    /// Removes a key from the dictionary
    /// </summary>
    public bool Remove(ReadOnlySpan<byte> key, AVDictFlags flags = 0)
    {
        bool existed = ContainsKey(key);
        if (existed)
        {
            unsafe
            {
                fixed(AVDictionary** ptr = &_handle)
                {
                    av_dict_set(ptr, key.RawHandle, null, (int)flags).CheckError();
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
                av_dict_free(ptr);
                av_dict_copy(ptr, null, 0);
            }
            base._handle = _handle; // Should be null after free
        }
    }

    /// <summary>
    /// Efficiently populates a dictionary from key-value pairs
    /// </summary>
    public static void Populate(FFHandleSource<AVDictionary> dict, Span2D<byte> options)
    {
        if (options.IsEmpty) return;


    }

    /// <summary>
    /// Copies entries from another dictionary
    /// </summary>
    public void CopyFrom(MediaDictionary other, AVDictFlags flags = 0)
    {
        unsafe
        {
            fixed (AVDictionary** prt = &_handle) {
                av_dict_copy(prt, other.Handle, (int)flags).CheckError();
            }
        }
    }
    
    public Enumerator GetEnumerator()
    {
        unsafe {
            return new Enumerator(Handle);
        }
    }
    
    
    
    public override string ToString()
    {
        return ToString((byte)':', (byte)'|');
    }

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

    protected override void Free()
    {
        unsafe
        {
            if (_handle != null)
            {
                fixed (AVDictionary** handle = &_handle) {
                    av_dict_free(handle);
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
                _currentEntry = av_dict_iterate(_dict, _currentEntry);
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
