using System.Diagnostics;

using FFmpeg.Wrapper;

if (args.Length < 1) {
    Console.WriteLine("Usage: AVEncode <output path>");
    return;
}
using var muxer = new MediaMuxer(args[0]);

var frameRate = new Rational(24, 1);
using var videoFrame = new VideoFrame(1280, 720, PixelFormats.YUV420P);
using var videoEnc = new VideoEncoder(MediaCodec.GetEncoder("libx264"), videoFrame.Format, frameRate);

//Set libx264 specific options
videoEnc.SetOption("crf", "24");
videoEnc.SetOption("preset", "faster");

//Note that some audio encoders support only a specific set of frame formats and sizes, 
//requiring use of `SwResampler` as done in the AVTranscode sample.
using var audioFrame = new AudioFrame(SampleFormats.FloatPlanar, 48000, 2, 1024) {
    PresentationTimestamp = 0
};
using var audioEnc = new AudioEncoder(CodecIds.AAC, audioFrame.Format, bitrate: 128_000);

var videoStream = muxer.AddStream(videoEnc);
var audioStream = muxer.AddStream(audioEnc);

var muxerOpts = new List<KeyValuePair<string, string>>();
if (args[0].EndsWith(".mp4")) {
    muxerOpts.Add(new("movflags", "+faststart"));
}
muxer.Open(muxerOpts); //Open encoders and write header

int numFrames = (int)(frameRate * 10); //Encode 10s of video
for (int i = 0; i < numFrames; i++) {
    videoFrame.PresentationTimestamp = videoEnc.GetFramePts(frameNumber: i);
    GenerateFrame(videoFrame);
    muxer.EncodeAndWrite(videoStream, videoEnc, videoFrame); //All in one: send_frame(), receive_packet(), interleaved_write()

    long samplePos = (long)(i / frameRate * audioEnc.SampleRate);
    while (audioFrame.PresentationTimestamp < samplePos) {
        GenerateAudio(audioFrame);
        muxer.EncodeAndWrite(audioStream, audioEnc, audioFrame);
        audioFrame.PresentationTimestamp += audioFrame.Count;
    }
}
//Flush frames delayed in the encoder
muxer.EncodeAndWrite(videoStream, videoEnc, null);
muxer.EncodeAndWrite(audioStream, audioEnc, null);

static void GenerateFrame(VideoFrame frame)
{
    Debug.Assert(frame.PixelFormat == PixelFormats.YUV420P);
    int ts = (int)frame.PresentationTimestamp!.Value;

    for (int y = 0; y < frame.Height; y++) {
        var rowY = frame.GetRowSpan<byte>(y, 0);

        for (int x = 0; x < frame.Width; x++) {
            rowY[x] = (byte)(x + y + ts * 3);
        }
    }
    for (int y = 0; y < frame.Height / 2; y++) {
        var rowU = frame.GetRowSpan<byte>(y, 1);
        var rowV = frame.GetRowSpan<byte>(y, 2);

        for (int x = 0; x < frame.Width / 2; x++) {
            rowU[x] = (byte)(128 + y + ts * 2);
            rowV[x] = (byte)(64 + x + ts * 5);
        }
    }
}
static void GenerateAudio(AudioFrame frame)
{
    Debug.Assert(frame.SampleFormat == SampleFormats.FloatPlanar && frame.NumChannels == 2);
    int samplePos = (int)frame.PresentationTimestamp!.Value;

    var samplesL = frame.GetSamples<float>(0);
    var samplesR = frame.GetSamples<float>(1);

    for (int i = 0; i < frame.Count; i++) {
        float a = MathF.Sin((samplePos + i) * (MathF.Tau * 440.0f / frame.SampleRate)) * 0.1f;
        samplesL[i] = a;
        samplesR[i] = a;
    }
}