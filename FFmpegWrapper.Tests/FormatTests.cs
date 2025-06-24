namespace FFmpegWrapper.Tests;

using FFmpeg.Wrapper;

using Xunit.Abstractions;

public class FormatTests(ITestOutputHelper testOutputHelper) : TestBase
{
    [Fact]
    public void Test()
    {
        testOutputHelper.WriteLine($"Okay: {ChannelLayout.GetDefault(14)}");
    }
}