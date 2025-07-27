namespace FFmpegWrapper.Filtering;

using System.Collections.Generic;

using Configuration;

public unsafe readonly struct MediaFilter
{
    public AVFilter* Handle { get; }

    public string Name => Helpers.PtrToStringUTF8(Handle->name)!;
    public string? Description => Helpers.PtrToStringUTF8(Handle->description)!;
    public MediaFilterFlags Flags => (MediaFilterFlags)Handle->flags;

    public int NumInputs => (int)ffmpeg.avfilter_filter_pad_count(Handle, 0);
    public int NumOutputs => (int)ffmpeg.avfilter_filter_pad_count(Handle, 1);

    public MediaFilter(AVFilter* handle) => Handle = handle;

    public static MediaFilter Get(string name)
    {
        var ptr = ffmpeg.avfilter_get_by_name(name);
        if (ptr == null) {
            throw new KeyNotFoundException("Unknown filter '" + name + "'");
        }
        return new MediaFilter(ptr);
    }

    /// <summary> Returns a list of parameters accepted during initialization of an instance of this filter. </summary>
    public IReadOnlyList<ContextOption> GetOptions(bool removeAliases = true)
        => ContextOption.GetOptions(&Handle->priv_class, removeAliases);

    public static IEnumerable<MediaFilter> GetRegisteredFilters()
    {
        void* iter;
        AVFilter* filter;
        var list = new List<MediaFilter>(768);

        while ((filter = ffmpeg.av_filter_iterate(&iter)) != null) {
            list.Add(new MediaFilter(filter));
        }
        return list;
    }

    public override string ToString() => Name;
}

