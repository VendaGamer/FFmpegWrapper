namespace FFmpegWrapper.Tests;

using FFmpegBindings.Linked;

public static class GlobalSetup
{
    [Before(TestSession)]
    public static void SetupGlobalInfrastructure()
    {
        FFmpegLinked.Init();
    }
}