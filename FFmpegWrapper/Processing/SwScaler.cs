namespace FFmpegWrapper.Processing;

using Flags;
using Media;

public sealed class SwScaler : FFObject<SwsContext>
{
    internal static readonly SwScaler Shared;
    static SwScaler()
    {
        var output = new PictureFormat(1920, 1080, AVPixelFormat.AV_PIX_FMT_YUV420P);
        var original = new PictureFormat(2560, 1440,  AVPixelFormat.AV_PIX_FMT_YUV420P);
        Shared = new SwScaler(original, output);
    }
    
    public PictureFormat InputFormat { get; private set; }
    public PictureFormat OutputFormat { get; private set; }

    public SwScaler(PictureFormat inFmt, PictureFormat outFmt, InterpolationMode flags = InterpolationMode.Bicubic)
    {
        unsafe
        {
            InputFormat = inFmt;
            OutputFormat = outFmt;

            _handle = ffmpeg.sws_getContext(inFmt.Width, inFmt.Height, inFmt.PixelFormat,
                outFmt.Width, outFmt.Height, outFmt.PixelFormat,
                (int)flags, null, null, null);

            if (_handle == null) {
                throw new OutOfMemoryException();
            }
        }
    }

    public bool Reinit(PictureFormat inFmt, PictureFormat outFmt, InterpolationMode flags = InterpolationMode.Bicubic)
    {
        if (inFmt.Equals(outFmt)) {
            return false;
        }

        if (OutputFormat.Equals(outFmt) && InputFormat.Equals(inFmt)) {
            return false;
        }
        
        unsafe
        {
            _handle = ffmpeg.sws_getCachedContext(_handle, inFmt.Width, inFmt.Height, inFmt.PixelFormat,
                outFmt.Width, outFmt.Height, outFmt.PixelFormat,
                (int)flags, null, null, null);
            
            InputFormat = inFmt;
            OutputFormat = outFmt;
        }

        return true;
    }

    /// <summary> Sets the input and output matrices to be used for YUV conversion. </summary>
    /// <remarks> Color transfer functions are ignored by swscale. Use the colorspace filter instead. </remarks>
    public void SetColorspace(in PictureColorspace input, in PictureColorspace output)
    {
        unsafe
        {
            ThrowIfDisposed();
            int* table, invTable;
            int srcRange, dstRange, brightness, contrast, saturation;
            ffmpeg.sws_getColorspaceDetails(_handle, &invTable, &srcRange, &table, &dstRange, &brightness, &contrast, &saturation);

            table = ffmpeg.sws_getCoefficients((int)input.Matrix);
            invTable = ffmpeg.sws_getCoefficients((int)output.Matrix);

            if (input.Range != AVColorRange.AVCOL_RANGE_UNSPECIFIED) {
                srcRange = input.Range == AVColorRange.AVCOL_RANGE_JPEG ? 1 : 0;
            }
            if (output.Range != AVColorRange.AVCOL_RANGE_UNSPECIFIED) {
                dstRange = output.Range == AVColorRange.AVCOL_RANGE_JPEG ? 1 : 0;
            }

            ffmpeg.sws_setColorspaceDetails(_handle, in *(int4*)invTable, srcRange, in *(int4*)table, dstRange, brightness, contrast, saturation);
        }
    }

    public void Convert(VideoFrame src, VideoFrame dst)
    {
        unsafe
        {
            Convert(src.Handle, dst.Handle);
        }
    }
    public unsafe void Convert(AVFrame* src, AVFrame* dst)
    {
        CheckFrame(src, InputFormat, input: true);
        CheckFrame(dst, OutputFormat, input: false);
        ffmpeg.sws_scale_frame(_handle, dst, src).CheckError();
    }

    /// <summary> Converts and rescales <paramref name="src"/> into the given frame. The input pixel format must be interleaved. </summary>
    /// <param name="stride"> The number of bytes per pixel line in <paramref name="src"/>. </param>
    public void Convert(ReadOnlySpan<byte> src, int stride, VideoFrame dst)
    {
        unsafe
        {
            CheckBuffer(src, stride, InputFormat, input: true);
            CheckFrame(dst.Handle, OutputFormat, input: false);

            fixed (byte* pSrc = src) {
                ffmpeg.sws_scale(Handle, [pSrc], [stride], 0, InputFormat.Height, dst.Handle->data, dst.Handle->linesize).CheckError();
            }
        }
    }

    /// <summary> Converts and rescales <paramref name="src"/> into the given buffer. The output pixel format must be interleaved. </summary>
    /// <param name="stride"> The number of bytes per pixel line in <paramref name="dst"/>. </param>
    public void Convert(VideoFrame src, Span<byte> dst, int stride)
    {
        unsafe
        {
            CheckFrame(src.Handle, InputFormat, input: true);
            CheckBuffer(dst, stride, OutputFormat, input: false);
        
            fixed (byte* pDst = dst) {
                ffmpeg.sws_scale(Handle, src.Handle->data, src.Handle->linesize, 0, src.Height, [pDst], [stride]).CheckError();
            }
        }
    }

    /// <summary> Converts and rescales <paramref name="src"/> into the given buffer. The input and output pixel formats must be interleaved. </summary>
    /// <param name="srcStride"> The number of bytes per pixel line in <paramref name="src"/>. </param>
    /// <param name="dstStride"> The number of bytes per pixel line in <paramref name="dst"/>. </param>
    public void Convert(ReadOnlySpan<byte> src, int srcStride, Span<byte> dst, int dstStride)
    {
        unsafe
        {
            CheckBuffer(src, srcStride, InputFormat, input: true);
            CheckBuffer(dst, dstStride, OutputFormat, input: false);

            fixed (byte* pSrc = src)
            fixed (byte* pDst = dst) {
                ffmpeg.sws_scale(Handle, [pSrc], [srcStride], 0, InputFormat.Height, [pDst], [dstStride]).CheckError();
            }
        }
    }

    private static unsafe void CheckFrame(AVFrame* frame, in PictureFormat format, bool input)
    {
        if (frame->format != (int)format.PixelFormat || frame->width != format.Width || frame->height != format.Height) {
            throw new ArgumentException((input ? "Input" : "Output") + " frame must match rescaler format");
        }
    }

    private static void CheckBuffer(ReadOnlySpan<byte> buffer, int stride, in PictureFormat format, bool input)
    {
        if (format.IsPlanar || buffer.Length < (long)format.Height * stride || 
            stride < ffmpeg.av_image_get_linesize(format.PixelFormat, format.Width, 0)
        ) {
            throw new ArgumentException((input ? "Input" : "Output") + " buffer must match rescaler format");
        }
    }

    protected override unsafe void Free()
    {
        if (_handle != null) {
            ffmpeg.sws_freeContext(_handle);
            _handle = null;
        }
    }
}