namespace FFmpegWrapper.Tests;

using Media;

public class FormatTests
{
    [Test]
    public async Task Test()
    {
        await Assert.That(ChannelLayout.GetDefault(14).ToString() is "9.1.4").IsTrue();
    }
}