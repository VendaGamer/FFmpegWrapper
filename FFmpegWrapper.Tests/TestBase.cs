namespace FFmpegWrapper.Tests;

using FFmpegBindings.Linked;

public abstract class TestBase
{
    private static volatile bool s_isInit;
    protected TestBase()
    {
        if (!s_isInit) {
            FFmpegLinked.Init();
            s_isInit = true;
        }
    }
}