namespace FFmpegWrapper.Media;

using System.Collections;
using Extensions;

/// <summary>
/// Useful wrapper for 
/// </summary>  
public readonly struct MediaDictionary<TSource> : IEnumerable<Utf8KeyValue>
    where TSource : IHandleSource<AVDictionary>
{
    public unsafe int Count {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            return av_dict_count(HandleSource.GetPinnableReference());
        }
    }

    internal readonly TSource HandleSource;
    
    public MediaDictionary(TSource handleSource)
    {
        HandleSource = handleSource;
    }
    
    public ReadOnlySpan<byte> this[ReadOnlySpan<byte> key] {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => GetValue(key);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => SetValue(key, value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long SetIntValue(ReadOnlySpan<byte> key, long value,
        AVDictFlags flags = AVDictFlags.AV_DICT_MATCH_CASE)
    {
        unsafe
        {
            fixed(AVDictionary** ptr = HandleSource)
                av_dict_set_int(ptr, key.RawHandle, value, (int)flags).CheckError();
            return value;
        }
    }
    
    /// <summary>
    /// Tries to get a value, returning false if the key doesn't exist
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetValue(ReadOnlySpan<byte> key, out ReadOnlySpan<byte> value, AVDictFlags flags = 0)
    {
        value = GetValue(key, flags);
        return value.IsEmpty;
    }
    
    /// <summary>
    /// Clears all entries from the dictionary
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear()
    {
        unsafe
        {
            fixed (AVDictionary** ptr = HandleSource) {
                av_dict_free(ptr);
                av_dict_set(ptr,null , null,0);
            }
        }
    }


    /// <summary>
    /// Gets the value associated with the given key, or null if there is no match.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe ReadOnlySpan<byte> GetValue(ReadOnlySpan<byte> key, AVDictFlags flags = AVDictFlags.AV_DICT_MATCH_CASE)
    {
        var entry = av_dict_get(HandleSource.GetPinnableReference(), key.RawHandle, null, (int)flags);
        return entry is null ? default : FFHelper.Utf8SpanFromPtrNullTerm(entry->value);
    }
    
    /// <summary>
    /// Sets the value associated with the given key, overwriting it if necessary.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetValue(ReadOnlySpan<byte> key, ReadOnlySpan<byte> value, AVDictFlags flags = AVDictFlags.AV_DICT_MATCH_CASE)
    {
        unsafe
        {
            fixed (AVDictionary** ptr = HandleSource) {
                av_dict_set(ptr, key.RawHandle, value.RawHandle, (int)flags).CheckError();
            }
        }
    }
    
    /// <summary>
    /// Copies entries from another dictionary
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CopyFrom(Handle<AVDictionary> other, AVDictFlags flags = AVDictFlags.AV_DICT_APPEND)
    {
        unsafe
        {
            fixed (AVDictionary** ptr = HandleSource) {
                av_dict_copy(ptr, other, (int)flags).CheckError();
            }
        }
    }
    
    /// <summary>
    /// Checks if a key exists
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ContainsKey(ReadOnlySpan<byte> key)
    {
        unsafe
        {
            return av_dict_get(HandleSource.GetPinnableReference(), key.RawHandle, null, 0) is not null;
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Enumerator GetEnumerator()
    {
        unsafe
        {
            return new Enumerator(HandleSource.GetPinnableReference());
        }
    }

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
}
