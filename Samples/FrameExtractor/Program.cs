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
const string test = "https://pf-storage3.premiumcdn.net/134302754/BrESMkHwycEBc0JsrLfpqHMZhm2f9OUj9synCrqupkcgphl8hvahwpso0hj1cxwFBoTM9f4AhlMI2zsPwhvOsX2TEdUAYS5PU3jhm2UMqIMaFk4uXbT38.mp4?token=9Kv3tL7YfAqC&expires=1765734744&sparams=token%2Cpath%2Cexpires&signature=81616f161946eeb6537c5d34c57fe1bb73af1622"; 
service.SampleImages(test,@"C:\Users\Vencaa\Desktop\out", 10, TimeSpan.FromSeconds(10));