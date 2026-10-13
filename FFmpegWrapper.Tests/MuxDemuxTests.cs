namespace FFmpegWrapper.Tests;

using Media;
using Media.Packets;
using Media.Streams;

public class MuxDemuxTests
{
    [Test]
    public async Task MediaPacket()
    {
        var pkt = new MediaPacket(1024);
        await Assert.That(pkt.PresentationTimestamp).IsNull();
        await Assert.That(pkt.Data.Length).IsEqualTo(1024);

        pkt.PresentationTimestamp = 1234;
        pkt.SetData(new byte[2048]);

        await Assert.That(pkt.Handle.Ref.pts).IsEqualTo(1234);
        await Assert.That(pkt.Data.Length).IsEqualTo(2048);

        pkt.Dispose();
        await Assert.That(() => _ = pkt.Handle).Throws<ObjectDisposedException>();
    }

    [Test]
    public async Task PacketSideData_Integration()
    {
        using var packet = new MediaPacket();

        await Assert.That(packet.SideData.Count).IsEqualTo(0);

        var entry1 = packet.SideData.Add(AVPacketSideDataType.AV_PKT_DATA_DISPLAYMATRIX, 9 * 4);
        await Assert.That(packet.SideData.Count).IsEqualTo(1);
        await Assert.That(entry1.Data.Length).IsEqualTo(9 * 4);

        packet.SideData.Remove(AVPacketSideDataType.AV_PKT_DATA_DISPLAYMATRIX);
        await Assert.That(packet.SideData.Count).IsEqualTo(0);
    }

    [Test]
    public async Task DemuxMetadata()
    {
        var demuxer = new MediaDemuxer("Resources/BigBuckBunny.mp4"u8);
        
        await Assert.That(demuxer.TryFindBestStream(AVMediaType.AVMEDIA_TYPE_VIDEO, out MediaStream stream)).IsTrue();
        await Assert.That(stream.Duration).IsEqualTo(TimeSpan.FromSeconds(30));
        await Assert.That(demuxer.Streams.Length).IsEqualTo(2);

        await Assert.That(demuxer.Metadata["title"u8].IsEmpty).IsTrue();
        
        await Assert.That(stream.CodecPars.CodecId).IsEqualTo(AVCodecID.AV_CODEC_ID_H264);
        await Assert.That(stream.CodecPars.PictureFormat.PixelFormat).IsEqualTo(AVPixelFormat.AV_PIX_FMT_NONE);
        await Assert.That(stream.CodecPars.PictureFormat.Width).IsEqualTo(1280);
        await Assert.That(stream.CodecPars.PictureFormat.Height).IsEqualTo(720);

        demuxer.Dispose();
        await Assert.That(() => _ = demuxer.Handle).Throws<ObjectDisposedException>();
    }
}