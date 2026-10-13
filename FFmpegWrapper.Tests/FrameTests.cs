namespace FFmpegWrapper.Tests;

using Media;
using Media.Frames;

public class FrameTests
{
    [Test]
    public async Task Video_Props()
    {
        var frame = new VideoFrame(1280, 720, AVPixelFormat.AV_PIX_FMT_RGBA);
        
        await Assert.That(frame.Format.Width is 1280).IsTrue();
        await Assert.That(frame.Format.Height is 720).IsTrue();
        await Assert.That(frame.Format.PixelFormat is AVPixelFormat.AV_PIX_FMT_RGBA).IsTrue();
        await Assert.That(frame.Format.IsPlanar).IsFalse();
        await Assert.That(frame.GetRowSpan<uint>(0).Length is 1280).IsTrue();

        var pixels = frame.GetPlaneSpan<uint>(0, out int stride);
        await Assert.That(pixels.Length is 1280 * 720).IsTrue();
        
        Assert.Throws<ArgumentOutOfRangeException>(() => frame.GetRowSpan<uint>(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => frame.GetRowSpan<uint>(720));

        frame.Dispose();
        Assert.Throws<ObjectDisposedException>(() => _ = frame.Handle);
    }

    [Test]
    public async Task Audio_Props()
    {
        var frame = new AudioFrame(AVSampleFormat.AV_SAMPLE_FMT_FLTP, 48000, 2, 1024);

        await Assert.That(frame.Capacity is 1024).IsTrue();
        await Assert.That(frame.Count is 1024).IsTrue();
        await Assert.That(frame.ChannelLayout.NumChannels is 2).IsTrue();
        await Assert.That(frame.SampleRate is 48000).IsTrue();
        await Assert.That(frame.IsPlanar).IsTrue();

        await Assert.That(frame.GetSamples<float>(0).Length is 1024).IsTrue();

        Assert.Throws<ArgumentOutOfRangeException>(() => frame.GetSamples<float>(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => frame.GetSamples<float>(2));

        frame.Dispose();
        Assert.Throws<ObjectDisposedException>(() => _ = frame.Handle);
    }

    [Test]
    public async Task ChannelLayout_Props()
    {
        var a = ChannelLayout.GetDefault(2);
        await Assert.That(a.NumChannels is 2).IsTrue();
        await Assert.That(a.Order is AVChannelOrder.AV_CHANNEL_ORDER_NATIVE).IsTrue();
        await Assert.That(a.ToString() is "stereo").IsTrue();

        var b = ChannelLayout.FromString("FL+FC+FR"u8);
        await Assert.That(b.NumChannels is 3).IsTrue();
        await Assert.That(b.Order is AVChannelOrder.AV_CHANNEL_ORDER_CUSTOM).IsTrue();
        await Assert.That(b.GetChannel(0) is AVChannel.AV_CHAN_FRONT_LEFT).IsTrue();
        await Assert.That(b.GetChannel(1) is AVChannel.AV_CHAN_FRONT_CENTER).IsTrue();
        await Assert.That(b.GetChannel(2) is AVChannel.AV_CHAN_FRONT_RIGHT).IsTrue();
    }

    [Test]
    public async Task SideData_Integration()
    {
        using var frame = new VideoFrame(128, 128, AVPixelFormat.AV_PIX_FMT_RGBA);

        await Assert.That(frame.SideData.Count is 0).IsTrue();

        var entry1 = frame.SideData.Add(AVFrameSideDataType.AV_FRAME_DATA_DISPLAYMATRIX, 9 * 4);
        var entry2 = frame.SideData.Add(AVFrameSideDataType.AV_FRAME_DATA_DETECTION_BBOXES, 128);
        await Assert.That(frame.SideData.Count is 2).IsTrue();
        await Assert.That(entry1.Data.Length is 9 * 4).IsTrue();

        frame.SideData.Remove(AVFrameSideDataType.AV_FRAME_DATA_DISPLAYMATRIX);
        await Assert.That(frame.SideData.Count is 1).IsTrue();
    }
}
