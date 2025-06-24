namespace FFmpegWrapper.Tests;

using FFmpeg.AutoGen.Bindings.DynamicallyLoaded;

public abstract class TestBase
{
    protected TestBase()
    {
        DynamicallyLoadedBindings.LibrariesPath = @"C:\ffmpeg\";
        DynamicallyLoadedBindings.Initialize();
    }
}