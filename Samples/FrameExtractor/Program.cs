using FFmpegBindings.Linked;
using FrameExtractor;

FFmpegLinked.Init();

var service = new FFMpegService();
const string test = "http://commondatastorage.googleapis.com/gtv-videos-bucket/sample/BigBuckBunny.mp4";

service.SampleImages(test, args[0], "png", 20, TimeSpan.FromSeconds(8));