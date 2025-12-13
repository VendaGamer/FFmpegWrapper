namespace FFmpegWrapper.Filtering;

using System.Collections.Generic;
using Configuration;
using Extensions;

public unsafe readonly struct MediaFilter
{
    public AVFilter* Handle { get; }

    public string Name => FFHelper.PtrToStringUtf8(Handle->name)!;
    public string? Description => FFHelper.PtrToStringUtf8(Handle->description)!;
    public AVFILTER_FLAGS Flags => (AVFILTER_FLAGS)Handle->flags;

    public int NumInputs => (int)avfilter_filter_pad_count(Handle, 0);
    public int NumOutputs => (int)avfilter_filter_pad_count(Handle, 1);

    public MediaFilter(AVFilter* handle) => Handle = handle;

    public static MediaFilter Get(ReadOnlySpan<byte> name)
    {
        var ptr = avfilter_get_by_name(name.RawHandle);
        
        return ptr is null ?
            throw new KeyNotFoundException($"Unknown filter named: {FFHelper.SpanToStringUtf8(name)}") :
            new MediaFilter(ptr);
    }

    /// <summary> Returns a list of parameters accepted during initialization of an instance of this filter. </summary>
    public IReadOnlyList<ContextOption> GetOptions(bool removeAliases = true)
        => ContextOption.GetOptions(&Handle->priv_class, removeAliases);

    public static IEnumerable<MediaFilter> GetRegisteredFilters()
    {
        void* iter;
        AVFilter* filter;
        var list = new List<MediaFilter>(768);

        while ((filter = av_filter_iterate(&iter)) != null) {
            list.Add(new MediaFilter(filter));
        }
        return list;
    }

    public override string ToString() => Name;
}

