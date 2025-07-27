

// https://github.com/FFmpeg/FFmpeg/blob/master/doc/examples/decode_filter_video.c

using FFmpegWrapper.Codecs.Decoding;
using FFmpegWrapper.Codecs.Encoding;
using FFmpegWrapper.Core;
using FFmpegWrapper.Filtering;
using FFmpegWrapper.Media;
using FFmpegWrapper.Media.Frames;
using FFmpegWrapper.Media.Packets;

if (args.Length < 2) {
    Console.WriteLine("Usage: DecodeFilterVideo <input path> <output path> <filter desc>?");
    return;
}

using var demuxer = new MediaDemuxer(args[0]);
using var muxer = new MediaMuxer(args[1]);
string filters = args.Length > 2 ? args[2] : "transpose=clock";

demuxer.TryFindBestStream(MediaTypes.Video, out var inputStream);

using var decoder = (VideoDecoder)demuxer.CreateStreamDecoder(inputStream);

using var filter = MediaFilterPipeline.CreateBuilder()
            .VideoBufferSource(decoder, autoRotate: true)
            .Segment(filters)
            .Build();

var frameRate = demuxer.GuessFrameRate(inputStream);
using var encoder = new VideoEncoder(CodecIds.H264, filter.GetVideoSink("out")!.Format, frameRate);
encoder.TimeBase = decoder.TimeBase;
var outputStream = muxer.AddStream(encoder);
muxer.Open();

using var packet = new MediaPacket();
using var frame = new VideoFrame(decoder.FrameFormat);
var endTime = TimeSpan.FromSeconds(10);

while (demuxer.Read(packet)) {
    if (packet.StreamIndex != outputStream.Index) continue;
    if (inputStream.GetTimestamp(packet.PresentationTimestamp!.Value) >= endTime) break;

    decoder.SendPacket(packet);
    while (decoder.ReceiveFrame(frame)) {
        filter.Apply(frame);
        muxer.EncodeAndWrite(outputStream, encoder, frame);
    }
}

//Flush frames delayed in the encoder
muxer.EncodeAndWrite(outputStream, encoder, null);