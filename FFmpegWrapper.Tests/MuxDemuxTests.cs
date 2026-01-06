namespace FFmpegWrapper.Tests;

using Containers;

using Media;
using Media.Packets;

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
        var entry2 = packet.SideData.Add(AVPacketSideDataType.AV_PKT_DATA_PALETTE, AVPALETTE_SIZE);
        var entry3 = packet.SideData.Add(AVPacketSideDataType.AV_PKT_DATA_DISPLAYMATRIX, 9 * 4);
        Assert.Equal(2, packet.SideData.Count);

        Assert.Equal(9 * 4, entry1.Data.Length);

        packet.SideData.Remove(AVPacketSideDataType.AV_PKT_DATA_DISPLAYMATRIX);
        Assert.Equal(1, packet.SideData.Count);
    }

    [Fact]
    public void DemuxMetadata()
    {
        var demuxer = new MediaDemuxer("Resources/BigBuckBunny.mp4"u8);

        Assert.Equal(596, demuxer.Duration!.Value.TotalSeconds, 0);
        Assert.Equal(2, demuxer.Streams.Length);

        Assert.Equal("Test Media File"u8, demuxer.Metadata["title"u8]);

        demuxer.TryFindBestStream(AVMediaType.AVMEDIA_TYPE_VIDEO, out var vs);
        Assert.Equal(AVCodecID.AV_CODEC_ID_H264, vs.CodecPars.CodecId);
        Assert.Equal(AVPixelFormat.AV_PIX_FMT_YUV420P, vs.CodecPars.PictureFormat.PixelFormat);
        Assert.Equal(1280, vs.CodecPars.PictureFormat.Width);
        Assert.Equal(720, vs.CodecPars.PictureFormat.Height);

        demuxer.Dispose();
        Assert.Throws<ObjectDisposedException>(() => _ = demuxer.Handle);
    }
}