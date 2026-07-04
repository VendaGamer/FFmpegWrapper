using FFmpegBindings.Linked;
using FFmpegMiniAudioInterop.Core;
using FFmpegWrapper.Media;
using MiniAudioBindings.Linked;

FFmpegLinked.Init();
MiniAudioLinked.Init();

var demuxer = new MediaDemuxer("./test.mp3"u8);
demuxer.ReadMeta();

var device = new FFmpegAudioDevice(demuxer);

device.Start();


while (true) {
    
}