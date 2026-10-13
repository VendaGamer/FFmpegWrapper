namespace FFmpegWrapper.Tests;

using FFmpegBindings.Abstractions;

public class CodecTests
{
    [Test]
    public async Task AVCodec_Props()
    {
        var codec1 = MediaCodec.GetEncoder("mpeg2video"u8);
        var codec2 = MediaCodec.GetEncoder("libmp3lame"u8);

        await Assert.That(codec1.Name is "mpeg2video").IsTrue();
        await Assert.That(codec1.Type is AVMediaType.AVMEDIA_TYPE_VIDEO).IsTrue();
        await Assert.That(codec1.Id is AVCodecID.AV_CODEC_ID_MPEG2VIDEO).IsTrue();
        await Assert.That(codec1.IsEncoder).IsTrue();
        await Assert.That(codec1.IsDecoder).IsFalse();
        await Assert.That(codec1.SupportedPixelFormats[0] is AVPixelFormat.AV_PIX_FMT_YUV420P).IsTrue();

        await Assert.That(codec2.Name is "libmp3lame").IsTrue();
        await Assert.That(codec2.Type is AVMediaType.AVMEDIA_TYPE_AUDIO).IsTrue();
        await Assert.That(codec2.Id is AVCodecID.AV_CODEC_ID_MP3).IsTrue();
        await Assert.That(codec2.IsEncoder).IsTrue();
        await Assert.That(codec2.IsDecoder).IsFalse();
        await Assert.That(codec2.SupportedChannelLayouts[1].nb_channels is 2).IsTrue();
        await Assert.That(codec2.SupportedSampleFormats[1] is AVSampleFormat.AV_SAMPLE_FMT_FLTP).IsTrue();
        await Assert.That(codec2.SupportedSampleRates[0] is 44100).IsTrue();
    }

    [Test]
    public async Task AVCodec_GetRegistered()
    {
        var codecs = MediaCodec.AvailableCodecs;
        
        await Assert.That(codecs.IsEmpty).IsFalse();
        await Assert.That(codecs).Contains(static c => c.Id == AVCodecID.AV_CODEC_ID_H264);
    }
}