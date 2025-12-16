namespace FFmpegWrapper.Processing;

using CommunityToolkit.HighPerformance;

using Extensions;

using Media;

public sealed class SwScaler(FFHandle<SwsContext> handle) : FFObject<SwsContext>(handle)
{
    public SwScaler(PictureFormat inFmt, PictureFormat outFmt, SWSFlags flags = SWSFlags.SWS_BICUBIC)
        : this(Allocate(inFmt, outFmt, flags)) { }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe FFHandle<SwsContext> Allocate(PictureFormat inFmt, PictureFormat outFmt, SWSFlags flags = SWSFlags.SWS_BICUBIC)
        => sws_getContext(inFmt.Width, inFmt.Height, inFmt.PixelFormat,
            outFmt.Width, outFmt.Height, outFmt.PixelFormat,
            (int)flags, null, null, null);

    public bool Reinit(in PictureFormat inFmt, in PictureFormat outFmt, SWSFlags flags = SWSFlags.SWS_BICUBIC)
    {
        if (inFmt.Equals(outFmt)) {
            return false;
        }
        
        unsafe
        {
            _handle = sws_getCachedContext(_handle, inFmt.Width, inFmt.Height, inFmt.PixelFormat,
                outFmt.Width, outFmt.Height, outFmt.PixelFormat,
                (int)flags, null, null, null);
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
            sws_getColorspaceDetails(Handle, &invTable, &srcRange, &table, &dstRange, &brightness, &contrast, &saturation);

            table = sws_getCoefficients((int)input.Matrix);
            invTable = sws_getCoefficients((int)output.Matrix);

            if (input.Range is not AVColorRange.AVCOL_RANGE_UNSPECIFIED)
                srcRange = input.Range is AVColorRange.AVCOL_RANGE_JPEG ? 1 : 0;
            
            if (output.Range is not AVColorRange.AVCOL_RANGE_UNSPECIFIED)
                dstRange = output.Range is AVColorRange.AVCOL_RANGE_JPEG ? 1 : 0;

            sws_setColorspaceDetails(_handle, invTable, srcRange, table, dstRange, brightness, contrast, saturation);
        }
    }
    
    public void Convert(FFHandle<AVFrame> src, FFHandle<AVFrame> dst)
    {
        unsafe
        {
            sws_scale_frame(Handle, dst, src).CheckError();
        }
    }
    
    public VideoFrame Convert(FFHandle<AVFrame> src)
    {
        unsafe {
            var output = new VideoFrame();
            sws_scale_frame(Handle, dst, src).CheckError();
        }
    }

    protected override unsafe void Free()
    {
        if (_handle != null) {
            sws_freeContext(_handle);
            _handle = null;
        }
    }
}
