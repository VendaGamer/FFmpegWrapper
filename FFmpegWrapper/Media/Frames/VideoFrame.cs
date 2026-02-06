namespace FFmpegWrapper.Media.Frames;

using System.Diagnostics.CodeAnalysis;
using System.Text;

using Codecs;
using Codecs.Decoding;
using Codecs.Encoding;

using Extensions;

using Processing;

public sealed class VideoFrame : MediaFrame
{
    
    #region Properties

    public PictureFormat Format {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            ref var handle = ref Handle.Ref;
            
            return new PictureFormat(
                handle.width,
                handle.height,
                (AVPixelFormat)handle.format, 
                handle.sample_aspect_ratio);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set {
            ref var handle = ref Handle.Ref;
            
            handle.width = value.Width;
            handle.height = value.Height;
            handle.format = (int)value.PixelFormat;
            handle.sample_aspect_ratio = value.AspectRatio;
        }
    }
    
    
    public PictureColorspace Colorspace {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            ref var handle =  ref Handle.Ref;
                
            return new PictureColorspace(
                handle.colorspace,
                handle.color_primaries,
                handle.color_trc,
                handle.color_range,
                handle.chroma_location);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set {
            ref var handle = ref Handle.Ref;
                
            handle.colorspace = value.Matrix;
            handle.color_primaries = value.Primaries;
            handle.color_trc = value.Transfer;
            handle.color_range = value.Range;
            handle.chroma_location = value.Location;
        }
    }

    /// <summary> Whether this frame is attached to a hardware frame context. </summary>
    public bool IsHardwareFrame {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            unsafe
            {
                return Handle.Ref.hw_frames_ctx is not null;
            }
        }
    }

    /// <summary> Whether the frame rows are flipped. Alias for <c>RowSize[0] &lt; 0</c>. </summary>
    public bool IsVerticallyFlipped {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Handle.Ref.linesize[0] < 0;
    }

    #endregion
    
    #region Constructors
    
    /// <inheritdoc />
    public VideoFrame(int width, int height, AVPixelFormat fmt) : this()
    {
        unsafe
        {
            if (width <= 0 || height <= 0) {
                throw new ArgumentException("Invalid frame dimensions.");
            }
            
            _handle->width = width;
            _handle->height = height;
            _handle->format = (int)fmt;
            
            if (av_frame_get_buffer(_handle, 0) < 0) {
                
                fixed (AVFrame** ptr = &_handle) {
                    av_frame_free(ptr);
                }
                
                throw new Exception("Failed to allocate frame buffers.");
            }
        }
    }

    public VideoFrame(int width, int height, AVPixelFormat fmt, Rational aspectRatio) : this(width, height, fmt)
    {
        unsafe {
            _handle->sample_aspect_ratio = aspectRatio;
        }
    }

    public VideoFrame(PictureFormat fmt)
        : this(fmt.Width, fmt.Height, fmt.PixelFormat, fmt.AspectRatio)
    {

    }

    public VideoFrame(Handle<AVFrame> handle) : base(handle)
    {

    }

    /// Allocates an empty <see cref="AVFrame"/>
    public unsafe VideoFrame() : this(av_frame_alloc())
    {
        
    }
    
    #endregion

    #region Methods
    
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
            
            return new Span<T>((void*)Handle.Ref.data[plane][y * stride], stride);
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

            byte* data = _handle->data[plane];
            int rowSize = _handle->linesize[plane];

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
        unsafe {
            ref var handle = ref Handle.Ref;
            
            var size = (handle.width, handle.height);

            //https://github.com/FFmpeg/FFmpeg/blob/c558fcf41e2027a1096d00b286954da2cc4ae73f/libavutil/imgutils.c#L111
            if (plane == 0) {
                return size;
            }
            
            var desc = av_pix_fmt_desc_get((AVPixelFormat)handle.format);
            if (desc == null || (desc->flags & (int)AV_PIX_FMT_FLAGS.AV_PIX_FMT_FLAG_HWACCEL) is not 0) {
                throw new InvalidOperationException();
            }
            
            
            for (int i = 0; i < 4; i++) {
                if (desc->comp[i].plane != plane) continue;
                
                if (i is 1 or 2 && (desc->flags & (int)AV_PIX_FMT_FLAGS.AV_PIX_FMT_FLAG_RGB) is 0) {
                    size.width = CeilShr(size.width, desc->log2_chroma_w);
                    size.height = CeilShr(size.height, desc->log2_chroma_h);
                }
                return size;
            }
            
            throw new ArgumentOutOfRangeException(nameof(plane));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static int CeilShr(int x, int s) => (x + (1 << s) - 1) >> s;
        }
    }

    /// <summary> Attempts to create a hardware frame memory mapping. Returns null if the backing device does not support frame mappings. </summary>
    public bool TryMap(AV_HWFRAME_MAP flags, out VideoFrame? mappedFrame)
    {
        unsafe
        {
            ThrowIfDisposed();
            if (!IsHardwareFrame) {
                throw new InvalidOperationException("Cannot create mapping of non-hardware frame.");
            }

            var mapping = av_frame_alloc();
            int result = av_hwframe_map(mapping, _handle, (int)flags);

            if (result is 0) {
                mapping->width = _handle->width;
                mapping->height = _handle->height;
                mappedFrame = new VideoFrame(mapping);
                return true;
            }
            
            av_frame_free(&mapping);
            mappedFrame = null;
            return false;
        }
    }
    /// <summary> Copy data from this frame to <paramref name="dest"/>. At least one of <see langword="this"/> or <paramref name="dest"/> must be a hardware frame. </summary>
    public void TransferTo(VideoFrame dest)
    {
        unsafe{
            
            ThrowIfDisposed();
            if (!IsHardwareFrame) {
                throw new InvalidOperationException("TransferTo expects a hardware source frame.");
            }

            // 1) Pick a software pixel format compatible for transfer FROM hardware
            var formats = GetHardwareTransferFormats(AVHWFrameTransferDirection.AV_HWFRAME_TRANSFER_DIRECTION_FROM);
            if (formats.IsEmpty) {
                throw new InvalidOperationException("No transfer formats available from hardware frame.");
            }
            
            // Choose the first software format (avoid hw-accel formats)
            var swFmt = formats[0];

            // 2) Initialize destination frame’s geometry and format
            dest.Handle.Ref.width  = _handle->width;
            dest.Handle.Ref.height = _handle->height;
            dest.Handle.Ref.format = (int)swFmt;

            // 3) Allocate buffers for destination
            var alloc = av_frame_get_buffer(dest.Handle, 0);
            if (alloc < 0) {
                ((LavResult)alloc).ThrowIfError("Failed to allocate destination frame buffers");
            }

            // 4) Perform the transfer
            av_hwframe_transfer_data(dest.Handle, _handle, 0)
                .CheckError("Failed to transfer data from hardware frame");
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
                FFHelper.GetSpanFromSentinelTerminatedPtr(pFormats, AVPixelFormat.AV_PIX_FMT_NONE);
            
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
                &handle->data._0, (nint*)&handle->linesize._0,
                (AVPixelFormat)handle->format, handle->color_range,
                handle->width, handle->height
            ).CheckError("Failed to clear frame.");
        }
    }
    
    public void Save(string fileName, PictureFormat format)
    {
        var span = fileName.AsSpan();
        var extIndex = fileName.LastIndexOf('.');
        if (extIndex is -1)
            throw new ArgumentException("File name must contain an extension.", nameof(fileName));
        

        // NET STANDARD BYPASS
        unsafe {
            Span<byte> fileNameUtf8 = stackalloc byte[Encoding.UTF8.GetMaxByteCount(fileName.Length) + 1];
            var written = Encoding.UTF8.GetBytes(span.RawHandle, fileName.Length,
                fileNameUtf8.RawHandle,fileNameUtf8.Length);

            var outFor = OutputFormat.FindByExtension(fileNameUtf8.Slice(0, written + 1)); 
            
            Save(fileName, format, outFor);
        }
    }
    
    public void Save(string fileName, PictureFormat format, OutputFormat outputFormat)
    {
        ref var handle = ref Handle.Ref;
        
        if(handle.width <= 0 || handle.height <= 0)
            throw new InvalidOperationException("Frame has zero dimensions; ensure you decoded a video frame before calling Save().");
        
        if (format.Width <= 0 || format.Height <= 0)
            format = new PictureFormat(
                handle.width,
                handle.height,
                (AVPixelFormat)handle.format,
                handle.sample_aspect_ratio);
        
        if (IsHardwareFrame) {
            using var tmp = new VideoFrame();
            TransferTo(tmp);
            tmp.Save(fileName, format, outputFormat);
            return;
        }

        MediaCodec codec = MediaCodec.GetEncoder(outputFormat.VideoCodec);

        unsafe {
            var desc = av_pix_fmt_desc_get(format.PixelFormat);
            
            int hasAlpha = (int)(desc->flags & (ulong)AV_PIX_FMT_FLAGS.AV_PIX_FMT_FLAG_ALPHA);
            
            format = new PictureFormat(format.Width, format.Height, 
                avcodec_find_best_pix_fmt_of_list(codec.SupportedPixelFormats.RawHandle,
                    this.Format.PixelFormat, hasAlpha, null)
            );
            
        }

        
        using var tempFrame = new VideoFrame(format);
        using var encoder = new VideoEncoder(codec.Handle, format, Rational.One);
        encoder.Handle.Ref.strict_std_compliance = (int)FFCompliance.FF_COMPLIANCE_UNOFFICIAL;
        using var sws = new SwScaler(this.Format, format);
        
        tempFrame.Colorspace = Colorspace;
        
        encoder.Open();
        
        sws.SetColorspace(Colorspace, tempFrame.Colorspace);
        sws.Convert(Handle, tempFrame.Handle);
        
        encoder.SendFrame(tempFrame.Handle);
        
        using var packet = new MediaPacket();
        encoder.ReceivePacket(packet);
        packet.SaveData(fileName);
    }

    /// <summary> Decodes a single frame from the specified image or video file. </summary>
    /// <remarks> This method may be susceptible to DoS attacks. Do not use with untrusted inputs. </remarks>
    public static VideoFrame Load(ReadOnlySpan<byte> filename, TimeSpan? position = null)
    {
        using var demuxer = new MediaDemuxer(filename);
        
        if (!demuxer.TryFindBestStream(AVMediaType.AVMEDIA_TYPE_VIDEO, out var stream)) {
            throw new FormatException();
        }

        using var decoder = (VideoDecoder)demuxer.CreateStreamDecoder(stream.Handle);
        using var packet = new MediaPacket();

        var frame = new VideoFrame();

        if (position is not null && !demuxer.Seek(position.Value)) {
            //Position is past the stream duration, go back to the start or we won't get anything.
            demuxer.Seek(TimeSpan.Zero, AVSEEK_FLAGS.AVSEEK_FLAG_BACKWARD);
        }

        while (demuxer.Read(packet.Handle) >= 0) {
            if (packet.StreamIndex != stream.Index) continue;

            decoder.SendPacket(packet.Handle);

            if (decoder.ReceiveFrame(frame.Handle)) {
                return frame;
            }
        }

        frame.Dispose();
        throw new FormatException();
    }
    
    #endregion
    
}
