namespace FFmpegWrapper.Tests;

using FFmpegBindings.Linked;

public abstract class TestBase
{
    protected TestBase()
    {
        FFmpegLinked.Init();
    }
}