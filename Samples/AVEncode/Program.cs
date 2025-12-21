using FFmpegBindings.DynamicallyLinked;

using FFmpegWrapper.Codecs;
using FFmpegWrapper.Media.Formats;

FFmpegLinked.Init();

Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine(string.Join(", \n", ChannelLayout.StandardChannelLayouts));

Console.WriteLine();


Console.ForegroundColor = ConsoleColor.Yellow;
Console.WriteLine(string.Join(", \n", MediaCodec.AvailableCodecs));
Console.ReadLine();