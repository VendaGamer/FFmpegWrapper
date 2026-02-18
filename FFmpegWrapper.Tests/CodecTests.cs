namespace FFmpegWrapper.Tests;

using Codecs.Decoding;

using Core;

using FFmpegBindings.Abstractions;

using Media.Formats;

public class CodecTests : TestBase
{
    
    [Fact]
    public void AVCodec_Props()
    {
        var codec1 = MediaCodec.GetEncoder("mpeg2video"u8);
        var codec2 = MediaCodec.GetEncoder("libmp3lame"u8);

        Assert.Equal("mpeg2video", codec1.Name);
        Assert.Equal(AVMediaType.AVMEDIA_TYPE_VIDEO, codec1.Type);
        Assert.Equal(AVCodecID.AV_CODEC_ID_MPEG2VIDEO, codec1.Id);
        Assert.True(codec1.IsEncoder);
        Assert.False(codec1.IsDecoder);
        Assert.Equal(AVPixelFormat.AV_PIX_FMT_YUV420P, codec1.SupportedPixelFormats[0]);

        Assert.Equal("libmp3lame", codec2.Name);
        Assert.Equal(AVMediaType.AVMEDIA_TYPE_AUDIO, codec2.Type);
        Assert.Equal(AVCodecID.AV_CODEC_ID_MP3, codec2.Id);
        Assert.True(codec2.IsEncoder);
        Assert.False(codec2.IsDecoder);
        Assert.Equal(2, codec2.SupportedChannelLayouts[1].nb_channels);
        Assert.Equal(AVSampleFormat.AV_SAMPLE_FMT_FLTP, codec2.SupportedSampleFormats[1]);
        Assert.Equal(44100, codec2.SupportedSampleRates[0]);
    }

    [Fact]
    public void AVCodec_GetRegistered()
    {
        var codecs = MediaCodec.AvailableCodecs;
        
        Assert.NotEmpty(codecs);
        Assert.Contains(codecs, c => c.Id == AVCodecID.AV_CODEC_ID_H264);
    }
}