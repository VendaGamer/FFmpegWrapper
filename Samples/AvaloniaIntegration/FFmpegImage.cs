namespace AvaloniaIntegration;

using Avalonia.Platform;
using Avalonia.Rendering.Composition;

public class FFmpegImage : ICompositionGpuInterop 
{
    public CompositionGpuImportedImageSynchronizationCapabilities GetSynchronizationCapabilities(string imageHandleType)
    {
        var caps = CompositionGpuImportedImageSynchronizationCapabilities.
    }

    public ICompositionImportedGpuImage ImportImage(IPlatformHandle handle, PlatformGraphicsExternalImageProperties properties)
    {
        throw new NotImplementedException();
    }

    public ICompositionImportedGpuImage ImportImage(ICompositionImportableSharedGpuContextImage image)
    {
        throw new NotImplementedException();
    }

    public ICompositionImportedGpuSemaphore ImportSemaphore(IPlatformHandle handle)
    {
        throw new NotImplementedException();
    }

    public ICompositionImportedGpuImage ImportSemaphore(ICompositionImportableSharedGpuContextSemaphore image)
    {
        throw new NotImplementedException();
    }

    public IReadOnlyList<string> SupportedImageHandleTypes { get; }
    public IReadOnlyList<string> SupportedSemaphoreTypes { get; }
    public bool IsLost { get; }
    public byte[]? DeviceLuid { get; set; }
    public byte[]? DeviceUuid { get; set; }
}