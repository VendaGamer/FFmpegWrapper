namespace FFmpegWrapper.SkiaSharp.Extensions;

using System.Runtime.CompilerServices;

using Media.Formats;
using Media.Frames;

using Processing;

public static class VideoFrameExtensions
{
    extension(VideoFrame frame)
    {
        
        /// <summary>
        /// Creates an SKImage from the VideoFrame. The image holds a reference to the frame data.
        /// Ensure the VideoFrame is not disposed while using the returned SKImage.
        /// </summary>
        public SKImage ToSKImage()
        {
            unsafe {
                ref var handle = ref frame.Handle.Ref;
                
                var width = handle.width;
                var height = handle.height;
                var pixelFormat = (AVPixelFormat)handle.format;
                var (colorType, needsConversion) = GetSkiaColorType(pixelFormat);
                
                if (needsConversion) {
                    return ToSKImageWithConversion(frame, colorType);
                }
                
                var stride = handle.linesize[0];
                var dataPtr = (IntPtr)handle.data[0];
                
                if (stride < 0) {
                    stride = -stride;
                    dataPtr = (IntPtr)(handle.data[0] + stride * (height - 1));
                }
                
                var imageInfo = new SKImageInfo(
                    width,
                    height,
                    colorType,
                    SKAlphaType.Premul);
                
                
                return SKImage.FromPixelCopy(imageInfo, dataPtr, stride);
            }
        }
        
        /// <summary>
        /// Creates an SKImage that directly references the VideoFrame data (zero-copy).
        /// The VideoFrame MUST be kept alive as long as the SKImage is in use.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public SKImage ToSKImageNoCopy()
        {
            unsafe {
                ref var handle = ref frame.Handle.Ref;
        
                var width = handle.width;
                var height = handle.height;
                var pixelFormat = (AVPixelFormat)handle.format;
                var (colorType, needsConversion) = GetSkiaColorType(pixelFormat);
        
                if (needsConversion) {
                    return ToSKImageWithConversion(frame, colorType);
                }
        
                var stride = handle.linesize[0];
                var dataPtr = (IntPtr)handle.data[0];
        
                if (stride < 0) {
                    stride = -stride;
                    dataPtr = (IntPtr)(handle.data[0] + stride * (height - 1));
                }
        
                var imageInfo = new SKImageInfo(
                    width,
                    height,
                    colorType,
                    SKAlphaType.Premul);
                
                
                
                return SKImage.FromPixels(imageInfo, dataPtr, stride);
            }
        }
        

        /// <summary>
        /// Copies VideoFrame data into an existing SKBitmap.
        /// The bitmap must have matching dimensions.
        /// </summary>
        public void CopyTo(SKBitmap bitmap)
        {
            unsafe {
                if (bitmap.Width != frame.Format.Width || bitmap.Height != frame.Format.Height)
                    throw new ArgumentException("Bitmap dimensions must match frame dimensions");

                var colorType = bitmap.ColorType;
                var pixelFormat = (AVPixelFormat)frame.Handle.Ref.format;
                
                if (RequiresConversion(pixelFormat, colorType)) {
                    CopyWithConversion(frame, bitmap);
                    return;
                }
                
                var pixels = bitmap.GetPixels();
                var srcSpan = frame.GetPlaneSpan<byte>(0, out int srcStride);
                var dstStride = bitmap.RowBytes;
                var height = bitmap.Height;

                var src = frame.Data[0];
                var dst = (byte*)pixels;


                if (srcStride == dstStride && !frame.IsVerticallyFlipped) {
                    // Fast path
                    srcSpan.CopyTo(new Span<byte>(dst, srcSpan.Length));
                } else {
                    var rowSize = Math.Min(srcStride, dstStride);
                    for (int y = 0; y < height; y++) {
                        var srcRow = new Span<byte>(src + y * srcStride, rowSize);
                        var dstRow = new Span<byte>(dst + y * dstStride, rowSize);
                        srcRow.CopyTo(dstRow);
                    }
                }

                var pix = bitmap.PeekPixels();
                bitmap.NotifyPixelsChanged();
            }
        }
    }
    
    private static SKImage ToSKImageWithConversion(VideoFrame frame, SKColorType targetColorType)
    {
        var targetPixelFormat = targetColorType switch
        {
            SKColorType.Bgra8888 => AVPixelFormat.AV_PIX_FMT_BGRA,
            SKColorType.Rgba8888 => AVPixelFormat.AV_PIX_FMT_RGBA,
            SKColorType.Rgb888x => AVPixelFormat.AV_PIX_FMT_RGB0,
            _ => AVPixelFormat.AV_PIX_FMT_BGRA
        };
        
        var srcFormat = frame.Format;
        var dstFormat = new PictureFormat(
            srcFormat.Width, 
            srcFormat.Height, 
            targetPixelFormat,
            srcFormat.AspectRatio);
        
        using var convertedFrame = new VideoFrame(dstFormat);
        using var scaler = new SwScaler(srcFormat, dstFormat);
        

        scaler.SetColorspace(frame.Colorspace, convertedFrame.Colorspace);
        scaler.Convert(frame.Handle, convertedFrame.Handle);
        

        return convertedFrame.ToSKImage();
    }
    
    private static void CopyWithConversion(VideoFrame frame, SKBitmap bitmap)
    {
        var targetPixelFormat = bitmap.ColorType switch
        {
            SKColorType.Bgra8888 => AVPixelFormat.AV_PIX_FMT_BGRA,
            SKColorType.Rgba8888 => AVPixelFormat.AV_PIX_FMT_RGBA,
            _ => AVPixelFormat.AV_PIX_FMT_BGRA
        };
        
        var srcFormat = frame.Format;
        var dstFormat = new PictureFormat(
            srcFormat.Width, 
            srcFormat.Height, 
            targetPixelFormat,
            srcFormat.AspectRatio);
        
        using var convertedFrame = new VideoFrame(dstFormat);
        using var scaler = new SwScaler(srcFormat, dstFormat);
        
        scaler.SetColorspace(frame.Colorspace, convertedFrame.Colorspace);
        scaler.Convert(frame.Handle, convertedFrame.Handle);
        
        convertedFrame.CopyTo(bitmap);
    }
    
    private static (SKColorType colorType, bool needsConversion) GetSkiaColorType(AVPixelFormat format)
    {
        return format switch
        {
            AVPixelFormat.AV_PIX_FMT_BGRA => (SKColorType.Bgra8888, false),
            AVPixelFormat.AV_PIX_FMT_RGBA => (SKColorType.Rgba8888, false),
            AVPixelFormat.AV_PIX_FMT_RGB0 => (SKColorType.Rgb888x, false),
            _ => (SKColorType.Bgra8888, true)
        };
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool RequiresConversion(AVPixelFormat ffmpegFormat, SKColorType skiaType)
    {
        (SKColorType expectedType, bool needsConv) = GetSkiaColorType(ffmpegFormat);
        return needsConv || expectedType != skiaType;
    }
}