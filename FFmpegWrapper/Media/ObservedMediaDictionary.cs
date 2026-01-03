namespace FFmpegWrapper.Media;

using Extensions;

/// <summary>
/// Useful wrapper for 
/// </summary>
public readonly ref struct ObservedMediaDictionary : IFFHandleSourceObserver<AVDictionary>
{
    public unsafe int Count => av_dict_count(HandleSource);
    public unsafe FFHandleSource<AVDictionary> HandleSource => _handleSource;


    internal readonly unsafe AVDictionary** _handleSource;
    
    public ObservedMediaDictionary(FFHandleSource<AVDictionary> handleSource)
    {
        unsafe {
            _handleSource = handleSource;
        }
    }
    
    public ReadOnlySpan<byte> this[ReadOnlySpan<byte> key] {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => GetValue(key);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => Set(new Utf8KeyValue(key, value), AVDictFlags.AV_DICT_DONT_STRDUP_VAL);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe ReadOnlySpan<byte> GetValue(ReadOnlySpan<byte> key, AVDictFlags flags = 0)
    {
        var entry = av_dict_get(HandleSource, key.RawHandle, null, (int)flags);
        return entry is null ? default : FFHelper.Utf8SpanFromPtrNullTerm(entry->value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe void Set(Utf8KeyValue entry, AVDictFlags flags = 0) 
        => av_dict_set(_handleSource, entry.Key.RawHandle, entry.Value.RawHandle, (int)flags)
            .ThrowError("Could not set dictionary entry");
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear(AVDictFlags flags = 0)
    {
        unsafe {
            var handle = HandleSource.Raw;
            
            av_dict_free(handle);
            av_dict_copy(handle, null, 0).CheckError("Could not clear allocate new dictionary");
        }
    }
}
