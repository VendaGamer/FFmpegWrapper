using System.Diagnostics;

using FFmpegBindings.DynamicallyLinked;

using FFmpegWrapper.Codecs.Decoding;
using FFmpegWrapper.Core;
using FFmpegWrapper.Filtering;
using FFmpegWrapper.Media;
using FFmpegWrapper.Media.Frames;
using FFmpegWrapper.Media.Packets;

FFmpegLinked.Init();

var service = new FFMpegService();
const string test = "http://commondatastorage.googleapis.com/gtv-videos-bucket/sample/BigBuckBunny.mp4"; 
service.SampleImages(test,"/home/vencaa/Desktop/out/", 10, TimeSpan.FromSeconds(10));