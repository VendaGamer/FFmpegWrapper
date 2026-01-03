namespace FFmpegWrapper.Core;

using System.Text;

using Extensions;

public readonly struct Utf8KeyValue
{
    public unsafe ReadOnlySpan<byte> Key => FFHelper.Utf8SpanFromPtrNullTerm(_key);
    public unsafe ReadOnlySpan<byte> Value => FFHelper.Utf8SpanFromPtrNullTerm(_value);
        
    internal readonly unsafe byte* _key;
    internal readonly unsafe byte* _value;
        
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe Utf8KeyValue(ReadOnlySpan<byte> key, ReadOnlySpan<byte> value)
    {
        _key = key.RawHandle;
        _value = value.RawHandle;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal unsafe Utf8KeyValue(byte* key, byte* value)
    {
        _key = key;
        _value = value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe implicit operator Utf8KeyValue(AVDictionaryEntry entry) => new(entry.key, entry.value);

    public override unsafe string ToString()
    {
        return $"[ {Encoding.UTF8.GetString(_key, Key.Length)} , {Encoding.UTF8.GetString(_value, Value.Length)} ]";
    }
}