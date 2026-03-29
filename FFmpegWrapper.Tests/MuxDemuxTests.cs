namespace FFmpegWrapper.Tests;

using Media;
using Media.Packets;
using Media.Streams;

public unsafe class MuxDemuxTests : TestBase
{

    [Fact]
    public void MediaPacket()
    {
        var pkt = new MediaPacket(1024);
        Assert.Null(pkt.PresentationTimestamp);
        Assert.Equal(1024, pkt.Data.Length);

        pkt.PresentationTimestamp = 1234;
        pkt.SetData(new byte[2048]);

        Assert.Equal(1234, pkt.Handle.Ref.pts);
        Assert.Equal(2048, pkt.Data.Length);

        pkt.Dispose();
        Assert.Throws<ObjectDisposedException>(() => _ = pkt.Handle);
    }

    [Fact]
    public void PacketSideData_Integration()
    {
        using var packet = new MediaPacket();

        Assert.Equal(0, packet.SideData.Count);

        var entry1 = packet.SideData.Add(AVPacketSideDataType.AV_PKT_DATA_DISPLAYMATRIX, 9 * 4);
        Assert.Equal(1, packet.SideData.Count);
        Assert.Equal(9 * 4, entry1.Data.Length);

        packet.SideData.Remove(AVPacketSideDataType.AV_PKT_DATA_DISPLAYMATRIX);
        Assert.Equal(0, packet.SideData.Count);
    }

    [Fact]
    public void DemuxMetadata()
    {
        var demuxer = new MediaDemuxer("Resources/BigBuckBunny.mp4"u8);
        
        Assert.True(demuxer.TryFindBestStream(AVMediaType.AVMEDIA_TYPE_VIDEO, out MediaStream stream));
        Assert.Equal(stream.Duration, TimeSpan.FromSeconds(30));
        Assert.Equal(2, demuxer.Streams.Length);

        Assert.Equal(""u8, demuxer.Metadata["title"u8]);
        
        Assert.Equal(AVCodecID.AV_CODEC_ID_H264, stream.CodecPars.CodecId);
        Assert.Equal(AVPixelFormat.AV_PIX_FMT_NONE, stream.CodecPars.PictureFormat.PixelFormat);
        Assert.Equal(1280, stream.CodecPars.PictureFormat.Width);
        Assert.Equal(720, stream.CodecPars.PictureFormat.Height);

        demuxer.Dispose();
        Assert.Throws<ObjectDisposedException>(() => _ = demuxer.Handle);
    }
}