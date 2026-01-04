using System.Diagnostics;

using FFmpegBindings.Linked;
using FFmpegWrapper.Codecs;
using FFmpegWrapper.Core;
using FFmpegWrapper.Media;


FFmpegLinked.Init();

var decoderConfigs = CodecHardwareConfig.AvailableDecoderConfigs;
var encoderConfigs = CodecHardwareConfig.AvailableEncoderConfigs;
var dict = MediaDictionaryOwner.CreateFromEntries([
    new Utf8KeyValue("Popelka"u8, "1"u8),
    new Utf8KeyValue("Pooooooooooooo"u8, "9"u8)
]);

Console.ReadLine();