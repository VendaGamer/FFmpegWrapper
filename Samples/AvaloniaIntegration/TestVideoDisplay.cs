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
    private static readonly SKRuntimeEffect _colorConversionShader;
    private readonly Lock _frameLock = new();
    
    private VideoFrame? _currentFrame;
    private int _width;
    private int _height;

    static VideoFrameVisualHandler()
    {
        var sksl = """
                   uniform shader yPlane;
                   uniform shader uvPlane;
                   uniform float2 uvScale;
                   
                   half4 main(float2 coord) {
                       // Sample Y plane (luminance)
                       
                       half y = sample(yPlane, coord).r;
                       
                       // Sample UV plane (chrominance) - NV12 has UV interleaved
                       // Scale coordinates since UV plane is half resolution
                       half2 uv = sample(uvPlane, coord * uvScale).rg;
                       
                       // Convert from [0,1] range and apply YUV to RGB conversion
                       y = y - 0.0625;  // 16/255 for limited range
                       half u = uv.r - 0.5;
                       half v = uv.g - 0.5;
                       
                       // Apply conversion matrix
                       half r = y + 1.13983 * v;
                       half g = y - 0.39465 * u - 0.58060 * v;
                       half b = y + 2.03211 * u;
                       
                       return half4(r, g, b, 1.0);
                   }
                   """;
        
        var result = SKRuntimeEffect.Create(sksl, out var errors);
        _colorConversionShader = result ?? throw new Exception($"Shader compilation failed: {errors}");
    }

    public override void OnRender(ImmediateDrawingContext drawingContext)
    {
        VideoFrame? frame;
        lock (_frameLock)
        {
            frame = _currentFrame;
        }

        if (frame == null) return;

        if (!drawingContext.TryGetFeature<ISkiaSharpApiLeaseFeature>(out var leaseFeature))
            return;

        using var lease = leaseFeature.Lease();
        var canvas = lease.SkCanvas;

        try
        {
            RenderNV12Frame(canvas, frame);
        }
        catch (Exception ex)
        {
            // Log error but don't crash rendering pipeline
            System.Diagnostics.Debug.WriteLine($"Frame rendering error: {ex.Message}");
        }
    }
    
    private unsafe void RenderNV12Frame(SKCanvas canvas, VideoFrame frame)
    {
        var format = frame.Format;
        int width = format.Width;
        int height = format.Height;
        
        var yPlaneSize = frame.GetPlaneSize(0);
        var ySpan = frame.GetPlaneSpan<byte>(0, out var yStride);
        
        var uvPlaneSize = frame.GetPlaneSize(1);
        var uvSpan = frame.GetPlaneSpan<byte>(1, out var uvStride);
        
        fixed (byte* yPtr = ySpan)
        {
            var yInfo = new SKImageInfo(
                yPlaneSize.Width, 
                yPlaneSize.Height,
                SKColorType.Gray8,
                SKAlphaType.Opaque);

            using var yPixmap = new SKPixmap(yInfo, (IntPtr)yPtr, yStride * sizeof(byte));
            using var yImage = SKImage.FromPixels(yPixmap);
            
            fixed (byte* uvPtr = uvSpan)
            {
                var uvInfo = new SKImageInfo(
                    uvPlaneSize.Width,
                    uvPlaneSize.Height,
                    SKColorType.Rg88,
                    SKAlphaType.Opaque);

                using var uvPixmap = new SKPixmap(uvInfo, (IntPtr)uvPtr, uvStride * sizeof(byte));
                using var uvImage = SKImage.FromPixels(uvPixmap);
                
                var yShader = yImage.ToShader(SKShaderTileMode.Clamp, SKShaderTileMode.Clamp);
                var uvShader = uvImage.ToShader(SKShaderTileMode.Clamp, SKShaderTileMode.Clamp);
                
                var children = new SKRuntimeEffectChildren(_colorConversionShader)
                {
                    { "yPlane", yShader },
                    { "uvPlane", uvShader }
                };

                var shader = _colorConversionShader.ToShader(
                    false, 
                    new SKRuntimeEffectUniforms(_colorConversionShader), 
                    children);
                
                using var paint = new SKPaint();
                
                paint.Shader = shader;
                paint.FilterQuality = SKFilterQuality.Low;
                paint.IsAntialias = false;

                canvas.DrawRect(new SKRect(0, 0, width, height), paint);
            }
        }
    }

    
    public override void OnMessage(object message)
    {
        if (message is VideoFrame frame)
        {
            _currentFrame = frame;
            RegisterForNextAnimationFrameUpdate();
        }

        base.OnMessage(message);
    }
    
    public override void OnAnimationFrameUpdate()
    {
        if (_currentFrame is null)
            return;

        lock (_frameLock) {
            
        }
    }
    
    public override Rect GetRenderBounds() => new(0, 0, EffectiveSize.X, EffectiveSize.Y);
}