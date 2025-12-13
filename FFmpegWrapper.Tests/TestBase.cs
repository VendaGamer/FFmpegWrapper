namespace FFmpegWrapper.Tests;

using FFmpegBindings.DynamicallyLinked;

public abstract class TestBase
{
    protected TestBase()
    {
        FFmpegLinked.Init();
    }
}