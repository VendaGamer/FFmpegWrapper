using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using FFmpegBindings.Abstractions;
using FFmpegBindings.Linked;
using FFmpegWrapper.Codecs;
using FFmpegWrapper.Core;
using FFmpegWrapper.Media;


FFmpegLinked.Init();

var decoderConfigs = CodecHardwareConfig.AvailableDecoderConfigs;
var encoderConfigs = CodecHardwareConfig.AvailableEncoderConfigs;

var firstKey = "Popelka"u8;
var dict = MediaDictionaryOwner.CreateFromEntries([
    new Utf8KeyValue(firstKey, "1"u8),
    new Utf8KeyValue("Pooooooooooooo"u8, "9"u8)
]);