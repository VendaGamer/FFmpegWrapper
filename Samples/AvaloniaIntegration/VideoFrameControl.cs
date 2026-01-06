namespace AvaloniaIntegration;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Rendering.Composition;
using Avalonia.Threading;

using FFmpegWrapper.Media.Frames;

/// <summary>
/// Avalonia control for displaying VideoFrames
/// </summary>
public class VideoFrameControl : Control
{
    private CompositionCustomVisual? _customVisual;
    private VideoFrameVisualHandler? _handler;
    private VideoFrame? _currentFrame;

    static VideoFrameControl()
    {
        AffectsRender<VideoFrameControl>(StretchProperty);
    }

    public static readonly StyledProperty<Stretch> StretchProperty =
        AvaloniaProperty.Register<VideoFrameControl, Stretch>(nameof(Stretch), Stretch.Uniform);

    public Stretch Stretch
    {
        get => GetValue(StretchProperty);
        set => SetValue(StretchProperty, value);
    }

    /// <summary>
    /// Sets the video frame to display. The control will keep a reference until a new frame is set.
    /// </summary>
    public void SetFrame(VideoFrame? frame)
    {
        _currentFrame = frame;
        
        if (_handler != null)
        {
            Dispatcher.UIThread.Post(() => _handler.SetVideoFrame(frame));
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        var compositor = ElementComposition.GetElementVisual(this)?.Compositor;
        if (compositor == null) return;

        _customVisual = compositor.CreateCustomVisual(_handler = new VideoFrameVisualHandler());
        ElementComposition.SetElementChildVisual(this, _customVisual);

        _customVisual.Size = new Vector(Bounds.Width, Bounds.Height);
        
        // Set initial frame if available
        if (_currentFrame != null)
        {
            _handler.SetVideoFrame(_currentFrame);
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        
        _customVisual = null;
        _handler = null;
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        
        if (_customVisual != null)
        {
            _customVisual.Size = new Vector(e.NewSize.Width, e.NewSize.Height);
        }
    }
}