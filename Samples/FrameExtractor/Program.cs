using FFmpegBindings.DynamicallyLinked;

FFmpegLinked.Init();

var service = new FFMpegService();
const string test = "http://commondatastorage.googleapis.com/gtv-videos-bucket/sample/BigBuckBunny.mp4";
service.SampleImages(test, args[0], "jpg", 20, TimeSpan.FromSeconds(8));