using FFmpegBindings.Linked;
using FFmpegWrapper.Codecs;


FFmpegLinked.Init();

var decoderConfigs = CodecHardwareConfig.AvailableDecoderConfigs;
var encoderConfigs = CodecHardwareConfig.AvailableEncoderConfigs;


Console.ReadLine();