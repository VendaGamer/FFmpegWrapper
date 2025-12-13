namespace FFmpegWrapper.Media.Frames;

using Codecs.Decoding;
using Codecs.Encoding;
using Hardware;
using Processing;

public class VideoFrame : MediaFrame
{
    public int Width => Handle.Ref.width;
    public int Height => Handle.Ref.height;
    public AVPixelFormat PixelFormat => (AVPixelFormat)Handle.Ref.format;

    public PictureFormat Format => new(Width, Height, PixelFormat, Handle.Ref.sample_aspect_ratio);
    public PictureColorspace Colorspace {
        get {
            unsafe
            {
                return new PictureColorspace(_handle->colorspace, _handle->color_primaries,
                    _handle->color_trc, _handle->color_range);
                
            }
        }
        set {
            unsafe
            {
                ThrowIfDisposed();
            
                _handle->colorspace = value.Matrix;
                _handle->color_primaries = value.Primaries;
                _handle->color_trc = value.Transfer;
                _handle->color_range = value.Range;
            }
        }
    }

    /// <summary> Whether this frame is attached to a hardware frame context. </summary>
    public bool IsHardwareFrame {
        get {
            unsafe
            {
                return Handle.Ref.hw_frames_ctx is not null;
            }
        }
    }

    /// <summary> Whether the frame rows are flipped. Alias for <c>RowSize[0] &lt; 0</c>. </summary>
    public bool IsVerticallyFlipped {
        get {
            unsafe
            {
                return Handle.Ref.linesize[0] < 0;
            }
        }
    }

    /// <summary> Allocates an empty <see cref="AVFrame"/>. </summary>
    public VideoFrame()
    {
        unsafe
        {
            _handle = av_frame_alloc();
        }
    }

    /// Allocates an empty <see cref="AVFrame"/>
    public VideoFrame(PictureFormat fmt)
        : this(fmt.Width, fmt.Height, fmt.PixelFormat)
    {
        
    }

    /// <inheritdoc />
    public VideoFrame(int width, int height, AVPixelFormat fmt)
    {
        unsafe
        {
            if (width <= 0 || height <= 0) {
                throw new ArgumentException("Invalid frame dimensions.");
            }
            _handle = av_frame_alloc();
            _handle->format = (int)fmt;
            _handle->width = width;
            _handle->height = height;

            av_frame_get_buffer(_handle, 0).CheckError("Failed to allocate frame buffers.");
        }
    }
    /// <summary> Wraps an existing <see cref="AVFrame"/> pointer. </summary>
    /// <param name="takeOwnership">True if <paramref name="frame"/> should be freed when Dispose() is called.</param>
    public VideoFrame(FFHandle<AVFrame> frame)
    {
        unsafe
        {
            if (frame == null) {
                throw new ArgumentNullException(nameof(frame));
            }
            _handle = frame;
        }
    }

    /// <summary> Returns a view over the pixel row for the specified plane. </summary>
    /// <remarks> The returned span may be longer than <see cref="Width"/> due to padding. </remarks>
    /// <param name="y">Row index, in top to bottom order.</param>
    public Span<T> GetRowSpan<T>(int y, int plane = 0) where T : unmanaged
    {
        unsafe
        {
            if ((uint)y >= (uint)GetPlaneSize(plane).Height) {
                throw new ArgumentOutOfRangeException();
            }

            GetPlaneSpan<T>(plane, out int stride);
            
            return new Span<T>((void*)Handle.Ref.data[(uint)plane][y * stride],
                Math.Abs(stride / sizeof(T)));
            
        }
    }

    /// <summary> Returns a view over the pixel data for the specified plane. </summary>
    /// <remarks> Note that rows may be stored in reverse order depending on <see cref="IsVerticallyFlipped"/>. </remarks>
    /// <param name="stride">Number of pixels per row.</param>
    public Span<T> GetPlaneSpan<T>(int plane, out int stride) where T : unmanaged
    {
        unsafe
        {
            int height = GetPlaneSize(plane).Height;

            byte* data = _handle->data[(uint)plane];
            int rowSize = _handle->linesize[(uint)plane];

            if (rowSize < 0) {
                data += rowSize * (height - 1);
                rowSize *= -1;
            }
            stride = rowSize / sizeof(T);
            return new Span<T>(data, checked(height * stride));
        }
    }

    public (int Width, int Height) GetPlaneSize(int plane)
    {
        unsafe
        {
            ThrowIfDisposed();

            var size = (Width, Height);

            //https://github.com/FFmpeg/FFmpeg/blob/c558fcf41e2027a1096d00b286954da2cc4ae73f/libavutil/imgutils.c#L111
            if (plane == 0) {
                return size;
            }
            var desc = av_pix_fmt_desc_get(PixelFormat);
            
            /*if (desc == null || (desc->flags & AV_PIX_FMT_FLAG_HWACCEL) is not 0) {
                throw new InvalidOperationException();
            }
            
            for (uint i = 0; i < 4; i++) {
                if (desc->comp[i].plane != plane) continue;
                
                if (i is 1 or 2 && (desc->flags & AV_PIX_FMT_FLAG_RGB) is 0) {
                    size.Width = CeilShr(size.Width, desc->log2_chroma_w);
                    size.Height = CeilShr(size.Height, desc->log2_chroma_h);
                }
                return size;
            }*/
            
            throw new ArgumentOutOfRangeException(nameof(plane));

            static int CeilShr(int x, int s) => (x + (1 << s) - 1) >> s;
        }
    }

    /// <summary> Attempts to create a hardware frame memory mapping. Returns null if the backing device does not support frame mappings. </summary>
    public VideoFrame? Map(AV_HWFRAME_MAP flags)
    {
        unsafe
        {
            ThrowIfDisposed();
            if (!IsHardwareFrame) {
                throw new InvalidOperationException("Cannot create mapping of non-hardware frame.");
            }

            var mapping = av_frame_alloc();
            int result = av_hwframe_map(mapping, _handle, (int)flags);

            if (result == 0) {
                mapping->width = _handle->width;
                mapping->height = _handle->height;
                return new VideoFrame(mapping);
            }
            av_frame_free(&mapping);
            return null;
        }
    }
    /// <summary> Copy data from this frame to <paramref name="dest"/>. At least one of <see langword="this"/> or <paramref name="dest"/> must be a hardware frame. </summary>
    public void TransferTo(VideoFrame dest)
    {
        unsafe
        {
            ThrowIfDisposed();
            av_hwframe_transfer_data(dest.Handle, _handle, 0).CheckError("Failed to transfer data from hardware frame");
        }
    }

    /// <summary> Gets an array of possible source or dest formats usable in <see cref="TransferTo(VideoFrame)"/>. </summary>
    public ReadOnlySpan<AVPixelFormat> GetHardwareTransferFormats(AVHWFrameTransferDirection direction)
    {
        unsafe
        {
            ThrowIfDisposed();
            
            if (!IsHardwareFrame) {
                throw new InvalidOperationException("Cannot query transfer formats for non-hardware frame.");
            }

            AVPixelFormat* pFormats;

            if (av_hwframe_transfer_get_formats(_handle->hw_frames_ctx,
                    direction, &pFormats, 0) < 0) {
                return ReadOnlySpan<AVPixelFormat>.Empty;
            }
            
            var formats =
                FFHelper.GetSpanFromSentinelTerminatedPtr(pFormats, PixelFormats.None);
            
            av_freep(&pFormats);

            return formats;
        }
    }

    /// <summary> Fills this frame with black pixels. </summary>
    public void Clear()
    {
        unsafe {
            var handle = Handle.Raw;
            
            av_image_fill_black(
                handle->data, (nint*)handle->linesize,
                (AVPixelFormat)handle->format, handle->color_range,
                handle->width, handle->height
            ).CheckError("Failed to clear frame.");
        }
    }

    /// <summary> Saves this frame to the specified file. The format will be choosen based on the file extension. (Can be either JPG or PNG) </summary>
    /// <param name="quality">JPEG: Quantization factor. PNG: ZLib compression level. 0-100</param>
    public void Save(string filename, int quality = 90, int outWidth = 0, int outHeight = 0)
    {
        unsafe
        {
            ThrowIfDisposed();

            if (IsHardwareFrame) {
                using var tmp = new VideoFrame();
                TransferTo(tmp);
                tmp.Save(filename, quality, outWidth, outHeight);
                return;
            }
        
            bool jpeg = filename.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                        filename.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase);

            var codec = jpeg ? AVCodecID.AV_CODEC_ID_MJPEG : AVCodecID.AV_CODEC_ID_PNG;
            var pixFmt = jpeg ? AVPixelFormat.AV_PIX_FMT_YUV444P : AVPixelFormat.AV_PIX_FMT_RGBA;

            if (outWidth <= 0) outWidth = Width;
            if (outHeight <= 0) outHeight = Height;

            // SwScale fails to convert color range when pixel format and resolution are equal. See #6
            if (jpeg && pixFmt == PixelFormat && outWidth == Width && outHeight == Height) {
                using var rgbFrame = new VideoFrame(outWidth, outHeight, PixelFormats.RGBA);

                // This seems to be redundant, but keeping for good sake.
                rgbFrame.Colorspace = new PictureColorspace(AVColorSpace.AVCOL_SPC_RGB, AVColorPrimaries.AVCOL_PRI_BT470M, AVColorTransferCharacteristic.AVCOL_TRC_GAMMA22, AVColorRange.AVCOL_RANGE_JPEG);
                
                // TODO high quality
                SwScaler.Shared.Reinit(Format, rgbFrame.Format, SWSFlags.SWS_BILINEAR);
                SwScaler.Shared.SetColorspace(Colorspace, rgbFrame.Colorspace);
                SwScaler.Shared.Convert(Handle, rgbFrame.Handle);
            
                rgbFrame.Save(filename, quality, outWidth, outHeight);
                return;
            }

            using var tempFrame = new VideoFrame(outWidth, outHeight, pixFmt);
            using var encoder = new VideoEncoder(codec, tempFrame.Format, Rational.One);

            tempFrame.Colorspace = Colorspace;

            if (jpeg) {
                //1-31
                int q = 1 + (100 - quality) * 31 / 100;
                encoder.MaxQuantizer = q;
                encoder.MinQuantizer = q;
                encoder.Handle.Ref.color_range = AVColorRange.AVCOL_RANGE_JPEG;
                tempFrame.Handle.Ref.color_range = AVColorRange.AVCOL_RANGE_JPEG;
            } else {
                //zlib compression (0-9)
                encoder.CompressionLevel = quality * 9 / 100;
            }
        
            encoder.Open();

            var scalerMode = quality >= 80 ? SWSFlags.SWS_BICUBIC : SWSFlags.SWS_BILINEAR;
        
            SwScaler.Shared.Reinit(Format, tempFrame.Format, scalerMode);
            SwScaler.Shared.SetColorspace(this.Colorspace, tempFrame.Colorspace);
            SwScaler.Shared.Convert(Handle, tempFrame.Handle);

            encoder.SendFrame(tempFrame.Handle);
        
            using var packet = new MediaPacket();
            encoder.ReceivePacket(packet);
        
#if NET9_0_OR_GREATER
        File.WriteAllBytes(filename, packet.Data);
#elif NETSTANDARD2_1_OR_GREATER
        using var fs = new FileStream(filename, FileMode.Create, FileAccess.Write);
        fs.Write(packet.Data);
#else
            using var fs = new FileStream(filename, FileMode.Create, FileAccess.Write);
            using var us = new UnmanagedMemoryStream(packet.DataRaw, packet.DataLength);
            us.CopyTo(fs);
            
#endif
        }
    }

    /// <summary> Decodes a single frame from the specified image or video file. </summary>
    /// <remarks> This method may be susceptible to DoS attacks. Do not use with untrusted inputs. </remarks>
    public static VideoFrame Load(ReadOnlySpan<byte> filename, TimeSpan? position = null)
    {
        using var demuxer = new MediaDemuxer(filename);
        
        if (!demuxer.TryFindBestStream(MediaTypes.Video, out var stream)) {
            throw new FormatException();
        }

        using var decoder = (VideoDecoder)demuxer.CreateStreamDecoder(stream);
        using var packet = new MediaPacket();

        var frame = new VideoFrame();

        if (position is not null && !demuxer.Seek(position.Value)) {
            //Position is past the stream duration, go back to the start or we won't get anything.
            demuxer.Seek(TimeSpan.Zero, AVSEEK_FLAGS.AVSEEK_FLAG_BACKWARD);
        }

        while (demuxer.Read(packet)) {
            if (packet.StreamIndex != stream.Index) continue;

            decoder.SendPacket(packet);

            if (decoder.ReceiveFrame(frame)) {
                return frame;
            }
        }

        frame.Dispose();
        throw new FormatException();
    }
}
