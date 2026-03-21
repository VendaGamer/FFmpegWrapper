namespace FFmpegWrapper.Media;

using Entry = KeyValuePair<string, string>;

/// <summary> Wrapper for an existing <see cref="AVDictionaryEntry"/>. </summary>

public readonly struct MediaDictionaryEntry : IHandleObserver<AVDictionaryEntry>
{
    public Handle<AVDictionaryEntry> Handle {
        get {
            unsafe {
                return new Handle<AVDictionaryEntry>(_handle, new SkipValidation());
            }
        }
    }

    public string Key {
        get {
            unsafe
            {
                return FFHelper.PtrToStringUtf8(_handle->key);
            }
        }
    }

    public string Value {
        get {
            unsafe {
                return FFHelper.PtrToStringUtf8(_handle->value);
            }
        }
    }

    public Entry ToEntry() => new(Key, Value);
    
    private readonly unsafe AVDictionaryEntry* _handle;
    public unsafe MediaDictionaryEntry(AVDictionaryEntry* handle)
    {
        this._handle = handle;
    }
}
