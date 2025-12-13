namespace FFmpegWrapper.Processing;

using CommunityToolkit.HighPerformance;
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

    public SwScaler(PictureFormat inFmt, PictureFormat outFmt, SWSFlags flags = SWSFlags.SWS_BICUBIC)
    {
        unsafe
        {
            InputFormat = inFmt;
            OutputFormat = outFmt;

            _handle = sws_getContext(inFmt.Width, inFmt.Height, inFmt.PixelFormat,
                outFmt.Width, outFmt.Height, outFmt.PixelFormat,
                (int)flags, null, null, null);

            if (_handle == null) {
                throw new OutOfMemoryException();
            }
        }
    }

    public bool Reinit(in PictureFormat inFmt, in PictureFormat outFmt, SWSFlags flags = SWSFlags.SWS_BICUBIC)
    {
        if (inFmt.Equals(outFmt)) {
            return false;
        }

        if (OutputFormat.Equals(outFmt) && InputFormat.Equals(inFmt)) {
            return false;
        }
        
        unsafe
        {
            _handle = sws_getCachedContext(_handle, inFmt.Width, inFmt.Height, inFmt.PixelFormat,
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
            sws_getColorspaceDetails(_handle, &invTable, &srcRange, &table, &dstRange, &brightness, &contrast, &saturation);

            table = sws_getCoefficients((int)input.Matrix);
            invTable = sws_getCoefficients((int)output.Matrix);

            if (input.Range != AVColorRange.AVCOL_RANGE_UNSPECIFIED) {
                srcRange = input.Range == AVColorRange.AVCOL_RANGE_JPEG ? 1 : 0;
            }
            if (output.Range != AVColorRange.AVCOL_RANGE_UNSPECIFIED) {
                dstRange = output.Range == AVColorRange.AVCOL_RANGE_JPEG ? 1 : 0;
            }

            sws_setColorspaceDetails(_handle, invTable, srcRange, table, dstRange, brightness, contrast, saturation);
        }
    }
    
    public void Convert(FFHandle<AVFrame> src, FFHandle<AVFrame> dst)
    {
        unsafe
        {
            CheckFrame(src, InputFormat, input: true);
            CheckFrame(dst, OutputFormat, input: false);
            
            sws_scale_frame(Handle, dst, src).CheckError();
        }
    }

    /// <summary> Converts and rescales <paramref name="src"/> into the given frame. The input pixel format must be interleaved. </summary>
    /// <param name="stride"> The number of bytes per pixel line in <paramref name="src"/>. </param>
    public unsafe void Convert(byte** src, ReadOnlySpan<int> stride, FFHandle<AVFrame> dst)
    {
        unsafe
        {
            CheckBuffer(src, stride, InputFormat, input: true);
            CheckFrame(dst, OutputFormat, input: false);
            sws_scale(Handle, src,
                (int*)stride.DangerousGetReference(), 0, InputFormat.Height,
                dst.Ref.data,
            dst.Ref.linesize).CheckError();
        }
    }

    private static void CheckFrame(FFHandle<AVFrame> frame, in PictureFormat format, bool input)
    {
        if (frame.Ref.format != (int)format.PixelFormat ||
            frame.Ref.width != format.Width ||
            frame.Ref.height != format.Height) {
            
            throw new ArgumentException((input ? "Input" : "Output") + " frame must match rescaler format");
        }
    }

    private unsafe static void CheckBuffer(byte** buffer, ReadOnlySpan<int> stride, in PictureFormat format, bool input)
    {
        // if (format.IsPlanar || buffer.Length < (long)format.Height * stride || 
        //     stride < av_image_get_linesize(format.PixelFormat, format.Width, 0)
        // ) {
        //     throw new ArgumentException((input ? "Input" : "Output") + " buffer must match rescaler format");
        // }
    }

    protected override unsafe void Free()
    {
        if (_handle != null) {
            sws_freeContext(_handle);
            _handle = null;
        }
    }
}
