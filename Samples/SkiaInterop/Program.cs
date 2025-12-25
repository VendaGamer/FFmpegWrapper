using System.Text;

using FFmpegBindings.Abstractions;

using FFmpegWrapper.Codecs.Encoding;
using FFmpegWrapper.Media;
using FFmpegWrapper.Media.Formats;
using FFmpegWrapper.Media.Frames;
using FFmpegWrapper.Processing;

using SkiaSharp;

if (args.Length < 1) {
    Console.WriteLine("Usage: SkiaInterop <output path>");
    return;
}
using var muxer = new MediaMuxer(Encoding.UTF8.GetBytes(args[0]));

int frameRate = 30;

using var encoder = new VideoEncoder(
    AVCodecID.AV_CODEC_ID_H264,
    new PictureFormat(1280, 720, AVPixelFormat.AV_PIX_FMT_YUV420P),
    frameRate, bitrate: 1200_000);

using var frame = new VideoFrame(encoder.FrameFormat);

var stream = muxer.AddStream(encoder);
muxer.Open();

using var bitmap = new SKBitmap(frame.Width, frame.Height, SKColorType.Rgba8888, SKAlphaType.Premul);
using var canvas = new SKCanvas(bitmap);
using var scaler = new SwScaler(new PictureFormat(bitmap.Width, bitmap.Height, AVPixelFormat.AV_PIX_FMT_RGBA),
    frame.Format, SWSFlags.SWS_BILINEAR);

int numFrames = (frameRate * 10 + 1); //encode 10s of video
for (int i = 0; i < numFrames; i++) {
    Console.Write($"Generating frame {i}/{numFrames}\r");

    //Draw some weird stuff
    using var paint = new SKPaint();
    paint.IsAntialias = true;
    paint.Color = SKColors.Black;
    canvas.Clear(SKColors.White);
    var font = new SKFont(SKTypeface.Default, 14);
    
    canvas.DrawText("Frame #" + i, bitmap.Width,SKTextAlign.Right, font, paint);

    paint.ImageFilter = SKImageFilter.CreateDropShadow(2f, 2f, 4f, 4f, 0x70_000000);

    for (int j = 0; j < 40; j++) {
        float t = i / (float)frameRate * 0.8f + j / 40.0f;
        float x = j / 40.0f * bitmap.Width;
        float y = bitmap.Height * 0.7f + MathF.Sin(t * 5) * 150;

        paint.Color = SKColor.FromHsv((t * 200) % 360, 75, 90);
        canvas.DrawCircle(x, y, 32, paint);
    }
    paint.ImageFilter = null;

    paint.Shader = SKShader.CreatePerlinNoiseTurbulence(0.03f, 0.03f, 3, i / (float)frameRate * 1.3f);
    canvas.DrawRect(32, 32, 256, 256, paint);

    //Convert to YUV and encode
    canvas.Flush();
    scaler.Convert(bitmap.GetPixelSpan(), bitmap.RowBytes, frame);

    frame.PresentationTimestamp = encoder.GetFramePts(frameNumber: i);
    muxer.EncodeAndWrite(stream, encoder, frame);
}
//Flush delayed frames in the encoder
muxer.EncodeAndWrite(stream, encoder, null!);