namespace AvaloniaIntegration;

using Avalonia;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.Composition;
using Avalonia.Skia;
using FFmpegWrapper.Media.Frames;
using SkiaSharp;

/// <summary>
/// Custom visual handler for rendering VideoFrames efficiently in Avalonia
/// </summary>
public class VideoFrameVisualHandler : CompositionCustomVisualHandler
{
    private SKImage? _currentImage;
    private SKImage? _pendingImage;
    private VideoFrame? _currentFrame;
    private VideoFrame? _pendingFrame;
    
    private readonly Lock _lock = new();
    
    public class UpdateFrameMessage
    {
        public SKImage? Image { get; init; }
        public VideoFrame? Frame { get; init; }
    }

    public override void OnRender(ImmediateDrawingContext drawingContext)
    {
        if (_currentImage is null) return;

        if (!drawingContext.TryGetFeature<ISkiaSharpApiLeaseFeature>(out var leaseFeature))
            return;

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
    
    public override void OnMessage(object message)
    {
        if (message is UpdateFrameMessage updateMsg)
        {
            lock (_lock)
            {
                _pendingImage?.Dispose();
                _pendingFrame?.Dispose();
                
                _pendingImage = updateMsg.Image;
                _pendingFrame = updateMsg.Frame;
            }
            
            RegisterForNextAnimationFrameUpdate();
        }

        base.OnMessage(message);
    }
    
    public override void OnAnimationFrameUpdate()
    {
        lock (_lock)
        {
            if (_pendingImage != null)
            {
                _currentImage?.Dispose();
                _currentFrame?.Dispose();
                
                _currentImage = _pendingImage;
                _currentFrame = _pendingFrame;
                
                _pendingImage = null;
                _pendingFrame = null;
                
                Invalidate();
            }
        }
        
        base.OnAnimationFrameUpdate();
    }
    
    public override Rect GetRenderBounds() => new(0, 0, EffectiveSize.X, EffectiveSize.Y);
}