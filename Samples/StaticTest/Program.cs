using FFmpegBindings.Linked;
using FFmpegWrapper.Codecs;

FFmpegLinked.Init();

foreach (var codec in MediaCodec.AvailableCodecs) {
    Console.WriteLine(codec.ToString());
}