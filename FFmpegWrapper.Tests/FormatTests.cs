namespace FFmpegWrapper.Tests;

using Media;

public class FormatTests : TestBase
{
    [Fact]
    public void Test()
    {
        Assert.Equal("9.1.4", ChannelLayout.GetDefault(14).ToString());
    }
}