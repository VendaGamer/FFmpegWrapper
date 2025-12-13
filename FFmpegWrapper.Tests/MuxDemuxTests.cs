namespace FFmpegWrapper.Tests;

using Containers;

using Media;
using Media.Packets;

public unsafe class MuxDemuxTests
{
    [Fact]
    public void CustomIO_Read()
    {
        var mem = new MemoryStream([1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13], writable: false);
        var ioc = IOContext.CreateInputFromStream(mem, leaveOpen: false);

        Assert.Equal(mem.Length, avio_size(ioc.Handle));
        Assert.Equal(0x01_02_03_04_05_06_07_08ul, avio_rb64(ioc.Handle));

        avio_seek(ioc.Handle, 0, (int)SeekOrigin.Begin);
        Assert.Equal(0x01_02_03_04_05_06_07_08ul, avio_rb64(ioc.Handle));

        Assert.Equal(0x09_0A_0B_0Cu, avio_rb32(ioc.Handle));
        Assert.Equal(0x0D, avio_r8(ioc.Handle));

        avio_r8(ioc.Handle);
        Assert.Equal(1, ioc.Handle.Raw->eof_reached);

        ioc.Dispose();

        Assert.Throws<ObjectDisposedException>(() => _ = ioc.Handle);
        Assert.Throws<ObjectDisposedException>(() => _ = mem.Length);
    }

    [Fact]
    public void CustomIO_Write()
    {
        var mem = new MemoryStream();
        var ioc = IOContext.CreateOutputFromStream(mem, leaveOpen: true);

        avio_wb64(ioc.Handle, 0x01_02_03_04_05_06_07_08ul);
        avio_wb32(ioc.Handle, 0x09_0A_0B_0Cu);

        ioc.Flush();
        Assert.Equal(12, mem.Length);

        ioc.Dispose();

        Assert.Throws<ObjectDisposedException>(() => _ = ioc.Handle);
        Assert.Equal(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 }, mem.ToArray());
    }

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

        Assert.Equal(0, pkt.Data.Length);

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
        var demuxer = new MediaDemuxer("Resources/demux_test.mkv"u8);

        Assert.Equal(5, demuxer.Duration!.Value.TotalSeconds, 0);
        Assert.Equal(2, demuxer.Streams.Length);

        Assert.Equal("Test Media File"u8, demuxer.Metadata["title"u8]);

        demuxer.TryFindBestStream(MediaTypes.Video, out var vs);
        Assert.Equal(CodecIds.H264, vs.CodecPars.CodecId);
        Assert.Equal(PixelFormats.YUV420P, vs.CodecPars.PixelFormat);
        Assert.Equal(320, vs.CodecPars.Width);
        Assert.Equal(240, vs.CodecPars.Height);

        demuxer.Dispose();
        Assert.Throws<ObjectDisposedException>(() => _ = demuxer.Handle);
    }
}