namespace FFmpegWrapper.Processing;

using Media;

public sealed class SwScaler(Handle<SwsContext> handle) : FFObject<SwsContext>(handle)
{
    public SwScaler(PictureFormat inFmt, PictureFormat outFmt, SwsFlags flags = SwsFlags.SWS_BICUBIC)
        : this(Allocate(inFmt, outFmt, flags)) { }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe Handle<SwsContext> Allocate(PictureFormat inFmt, PictureFormat outFmt, SwsFlags flags = SwsFlags.SWS_BICUBIC)
        => sws_getContext(inFmt.Width, inFmt.Height, inFmt.PixelFormat,
            outFmt.Width, outFmt.Height, outFmt.PixelFormat,
            (int)flags, null, null, null);

    public bool Reinit(in PictureFormat inFmt, in PictureFormat outFmt, SwsFlags flags = SwsFlags.SWS_BICUBIC)
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
            var handle = Handle.Raw;
            
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
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe void Convert(Handle<AVFrame> src, Handle<AVFrame> dst) 
        => sws_scale_frame(Handle, dst, src).CheckError();
    
    public VideoFrame Convert(Handle<AVFrame> src)
    {
        unsafe {
            var output = new VideoFrame();
            sws_scale_frame(Handle, output.Handle, src).CheckError();
            
            return output;
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
