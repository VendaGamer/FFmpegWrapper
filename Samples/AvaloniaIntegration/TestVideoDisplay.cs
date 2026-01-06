namespace AvaloniaIntegration;

using Avalonia;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.Composition;
using Avalonia.Skia;

using FFmpegWrapper.Media.Frames;
using FFmpegWrapper.SkiaSharp.Extensions;

using SkiaSharp;

/// <summary>
/// Custom visual handler for rendering VideoFrames efficiently in Avalonia
/// </summary>
public class VideoFrameVisualHandler : CompositionCustomVisualHandler
{
    private SKImage? _currentImage;
    private bool _disposed;

    public void SetVideoFrame(VideoFrame? frame)
    {
        if (_disposed) return;
        
        _currentImage?.Dispose();
        _currentImage = null;

        if (frame != null)
        {
            _currentImage = frame.ToSKImage();
        }
        
        RegisterForNextAnimationFrameUpdate();
    }

    public override void OnRender(ImmediateDrawingContext drawingContext)
    {
        if (_disposed || _currentImage == null) return;

        if (drawingContext.TryGetFeature<ISkiaSharpApiLeaseFeature>(out var leaseFeature))
        {
            using var lease = leaseFeature.Lease();
            var canvas = lease.SkCanvas;
            
            var bounds = GetRenderBounds();
            

            var imageWidth = _currentImage.Width;
            var imageHeight = _currentImage.Height;
            var boundsWidth = (float)bounds.Width;
            var boundsHeight = (float)bounds.Height;

            var scale = Math.Min(boundsWidth / imageWidth, boundsHeight / imageHeight);
            var scaledWidth = imageWidth * scale;
            var scaledHeight = imageHeight * scale;
            
            var x = (boundsWidth - scaledWidth) / 2;
            var y = (boundsHeight - scaledHeight) / 2;

            var destRect = new SKRect(x, y, x + scaledWidth, y + scaledHeight);
            
            canvas.DrawImage(_currentImage, destRect);
        }
    }


    private Rect GetRenderBounds()
    {
        return new Rect(0, 0, EffectiveSize.X, EffectiveSize.Y);
    }

    public void Dispose()
    {
        _disposed = true;
        _currentImage?.Dispose();
        _currentImage = null;
    }
}