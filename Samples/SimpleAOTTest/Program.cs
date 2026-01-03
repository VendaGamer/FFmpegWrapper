using CommunityToolkit.HighPerformance;

using FFmpegBindings.Abstractions;
using FFmpegBindings.Linked;

using FFmpegWrapper.Media;


FFmpegLinked.Init();

var dict = MediaDictionaryOwner.CreateFromEntries([
    new("threads"u8, "1"u8),
    new("moms"u8, "1"u8),
    new("good"u8, "1"u8)
], AVDictFlags.AV_DICT_DONT_STRDUP_VAL | AVDictFlags.AV_DICT_DONT_STRDUP_KEY);


Console.ReadLine();