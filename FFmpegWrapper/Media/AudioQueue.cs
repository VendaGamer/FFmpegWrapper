namespace FFmpegWrapper.Media;

public class AudioQueue : OwnedObject<AVAudioFifo>
{
    /// <summary>
    /// Gets the audio sample format type used by this FIFO buffer.
    /// This determines the data type and bit depth of audio samples (e.g., float, 16-bit signed integer).
    /// </summary>
    /// <value>The AVSampleFormat enumeration value representing the sample format.</value>
    public readonly AVSampleFormat Format;

    /// <summary>
    /// Gets the number of audio channels configured for <see cref="AudioQueue"/>.
    /// This value determines the channel layout (e.g., 1 for mono, 2 for stereo, 6 for 5.1 surround).
    /// </summary>
    /// <value>The number of audio channels, typically ranging from 1 to 8 or more.</value>
    public readonly int NumChannels;
    /// <summary>
    /// Gets the current number of audio samples stored in the FIFO buffer per channel.
    /// This represents the amount of data available for reading.
    /// </summary>
    /// <value>The number of samples per channel currently buffered. Returns 0 if the buffer is empty.</value>
    /// <remarks>
    /// The total number of sample values in the buffer is Size × NumChannels for interleaved formats.
    /// This property queries the underlying FFmpeg audio FIFO for real-time buffer status.
    /// </remarks>
    public int Size {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            unsafe
            {
                return av_audio_fifo_size(_handle);
            }
        }
    }

    /// <summary>
    /// Gets the available space in the FIFO buffer for additional audio samples per channel.
    /// This represents how many more samples can be written before the buffer becomes full.
    /// </summary>
    /// <value>The number of additional samples per channel that can be written to the buffer.</value>
    /// <remarks>
    /// A return value of 0 indicates the buffer is full and cannot accept more data.
    /// This property queries the underlying FFmpeg audio FIFO for real-time space availability.
    /// </remarks>
    public int Space {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            unsafe
            {
                return av_audio_fifo_space(Handle);
            }
        }
    }

    /// <summary>
    /// Gets the total capacity of the FIFO buffer in samples per channel.
    /// This represents the maximum number of samples the buffer can hold per channel.
    /// </summary>
    /// <value>The sum of currently used space (Size) and available space (Space).</value>
    /// <remarks>
    /// This is a calculated property that combines Size and Space to determine the buffer's
    /// total allocated capacity. The capacity remains constant throughout the buffer's lifetime
    /// unless explicitly reallocated.
    /// </remarks>
    public int Capacity {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Space + Size;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public AudioQueue(AudioFormat fmt, int initialCapacity)
        : this(fmt.SampleFormat, fmt.NumChannels, initialCapacity) { }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe AudioQueue(AVSampleFormat fmt, int numChannels, int initialCapacity)
        : base(av_audio_fifo_alloc(fmt, numChannels, initialCapacity))
    {
        Format = fmt;
        NumChannels = numChannels;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(AudioFrame frame)
    {
        unsafe
        {
            if (frame.SampleFormat != Format || frame.ChannelLayout.NumChannels != NumChannels) {
                throw new ArgumentException("Incompatible frame format.", nameof(frame));
            }
            Write(frame.Data, frame.Count);
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write<T>(Span<T> src) where T : unmanaged
    {
        unsafe
        {
            CheckFormatForInterleavedBuffer(src.Length, sizeof(T));

            fixed (T* pSrc = src) {
                Write((byte**)&pSrc, src.Length / NumChannels);
            }
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe void Write(byte** channels, int count)
    {
        av_audio_fifo_write(Handle, (void**)channels, count);
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="frame"></param>
    /// <param name="count"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Read(AudioFrame frame, int count)
    {
        unsafe
        {
            if (count <= 0 || count > frame.Capacity) {
                throw new ArgumentOutOfRangeException(nameof(count));
            }
            if (frame.SampleFormat != Format || frame.ChannelLayout.NumChannels != NumChannels) {
                throw new InvalidOperationException("Incompatible frame format.");
            }
            return frame.Count = Read((void**)frame.Data, count);
        }
    }
    
    /// <summary>
    /// Reads audio samples from the FIFO buffer into a managed span of unmanaged type T.
    /// This method provides a type-safe wrapper around the native FFmpeg audio FIFO read operation,
    /// automatically handling pointer conversion and channel interleaving validation.
    /// </summary>
    /// <typeparam name="T">The unmanaged type representing audio sample data (e.g., float, short, int).
    /// Must be an unmanaged type that matches the audio format stored in the FIFO.</typeparam>
    /// <param name="dest">The destination span to receive the audio samples. The span length must be
    /// a multiple of the number of audio channels configured for this FIFO buffer.</param>
    /// <returns>The number of samples per channel that were actually read from the FIFO buffer.
    /// This may be less than requested if insufficient data is available in the buffer.
    /// Returns 0 if no data is available or if the buffer is empty.</returns>
    /// <remarks>
    /// This method validates that the destination buffer size is appropriate for interleaved audio data
    /// based on the configured number of channels. The actual number of bytes read will be
    /// (return_value * NumChannels * sizeof(T)).
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Read<T>(Span<T> dest) where T : unmanaged
    {
        unsafe
        {
            CheckFormatForInterleavedBuffer(dest.Length, sizeof(T));

            fixed (T* pDest = dest) {
                return Read((void**)&pDest, dest.Length / NumChannels);
            }
        }
    }
    
    /// <summary>
    /// Reads audio samples from the FIFO buffer into a native byte pointer array.
    /// This method provides direct access to the underlying FFmpeg av_audio_fifo_read function
    /// for maximum performance and flexibility when working with unmanaged audio data.
    /// </summary>
    /// <param name="dest">Pointer to an array of byte pointers, where each pointer represents
    /// a channel's audio data. For planar audio formats, each pointer should point to a separate
    /// buffer for each channel. For interleaved formats, typically only the first pointer is used.</param>
    /// <param name="count">The number of samples per channel to read from the FIFO buffer.
    /// The total amount of data read depends on the number of channels and sample format.</param>
    /// <returns>The number of samples per channel that were actually read from the FIFO buffer.
    /// This may be less than the requested count if insufficient data is available.
    /// Returns 0 if no data is available, the buffer is empty, or an error occurred.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe int Read(void** dest, int count)
    {
        return av_audio_fifo_read(Handle, dest, count);
    }

    /// <summary>
    /// Clears all audio data from the FIFO buffer, resetting it to an empty state.
    /// This operation removes all buffered samples and resets the buffer's read/write positions.
    /// </summary>
    /// <remarks>
    /// After calling this method, the Size property will return 0 and Space will return the full capacity.
    /// This is equivalent to discarding all buffered audio data without reading it.
    /// The buffer's capacity and configuration (format, channels) remain unchanged.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear()
    {
        unsafe
        {
            av_audio_fifo_reset(Handle);
        }
    }
    
    /// <summary>
    /// Removes a specified number of audio samples from the beginning of the FIFO buffer without reading them.
    /// This operation effectively discards the oldest buffered samples, making space for new data.
    /// </summary>
    /// <param name="count">The number of samples per channel to remove from the buffer.
    /// Must be between 0 and the current Size of the buffer.</param>
    /// <exception cref="InvalidOperationException">Thrown when the operation fails, typically when count
    /// exceeds the number of available samples in the buffer or when an internal error occurs.</exception>
    /// <remarks>
    /// This method is useful for implementing audio synchronization or when you need to discard
    /// old audio data without the overhead of reading it into a destination buffer.
    /// Unlike Clear(), this method allows selective removal of samples while preserving
    /// the remaining buffered data.
    /// The total number of sample values removed is count × NumChannels for interleaved formats.
    /// After successful completion, the Size property will be reduced by the specified count.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Drain(int count)
    {
        unsafe
        {
            av_audio_fifo_drain(Handle, count).CheckError();
        }
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected override unsafe void Free()
    {
        av_audio_fifo_free(Handle);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void CheckFormatForInterleavedBuffer(int length, int sampleSize)
    {
        if (av_get_bytes_per_sample(Format) != sampleSize ||
            av_sample_fmt_is_planar(Format) is not 0 ||
            length % NumChannels is not 0
        ) {
            throw new InvalidOperationException("Incompatible buffer format.");
        }
    }
}
