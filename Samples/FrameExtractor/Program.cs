using FFmpegBindings.DynamicallyLinked;

FFmpegLinked.Init();

var service = new FFMpegService();
const string test = "http://commondatastorage.googleapis.com/gtv-videos-bucket/sample/BigBuckBunny.mp4"; 
service.SampleImage(test, TimeSpan.FromSeconds(8),"/home/vencaa/Desktop/out/test.png");