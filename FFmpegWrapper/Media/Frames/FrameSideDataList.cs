namespace FFmpegWrapper.Media.Frames;

using System.Text;

public readonly struct FrameSideDataList : IHandleObserver<AVFrame>
{
    public Handle<AVFrame> Handle {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            unsafe {
                return new Handle<AVFrame>(_handle, new SkipValidation());
            }
        }
    }

    public int Count {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Handle.Ref.nb_side_data;
    }

    public FrameSideData this[int index] {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            if (index > Count || index < 0) {
                throw new ArgumentOutOfRangeException();
            }

            unsafe {
                return new FrameSideData((Handle<AVFrameSideData>)Handle.Raw->side_data[index]);
            }
        }
    }
    
    internal readonly unsafe AVFrame* _handle;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public FrameSideDataList(Handle<AVFrame> handle)
    {
        unsafe
        {
            _handle = handle;
        }
    }

    /// <summary> Returns the side data entry for the given type, or null if not present. </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGet(AVFrameSideDataType type, out FrameSideData sideData)
    {
        unsafe
        {
            NullableHandle<AVFrameSideData> entry = av_frame_get_side_data(Handle, type);
            
            if (!entry.IsNull) {
                sideData = new FrameSideData(entry.Handle);
                return true;
            }
            
            sideData = default;
            return false;
        }
    }

    /// <summary> Allocates and adds a new a side data entry. </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public FrameSideData Add(AVFrameSideDataType type, nuint size)
    {
        unsafe
        {
            NullableHandle<AVFrameSideData> entry = av_frame_new_side_data(Handle, type, size);
            
            if (!entry.IsNull) {
                return new FrameSideData(entry.Handle);
            }
            
            throw new Exception("Failed to create new side data");
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Remove(AVFrameSideDataType type)
    {
        unsafe {
            var handle = Handle.Raw;
            int prevCount = handle->nb_side_data;
            
            av_frame_remove_side_data(handle, type);
            
            return handle->nb_side_data != prevCount;
        }
    }

    public void Clear()
    {
        unsafe
        {
            // https://github.com/FFmpeg/FFmpeg/blob/4e120fbbbd087c3acbad6ce2e8c7b1262a5c8632/libavfilter/f_sidedata.c#L117

            var handle = Handle.Raw;
        
            while (handle->nb_side_data is not 0) {
                av_frame_remove_side_data(handle, handle->side_data[0]->type);
            }
        }
    }

    /// <summary> Returns the value of an <see cref="AVFrameSideDataType.AV_FRAME_DATA_DISPLAYMATRIX"/> entry. </summary>
    public ReadOnlySpan<int> GetDisplayMatrix()
    {
        if (TryGet(AVFrameSideDataType.AV_FRAME_DATA_DISPLAYMATRIX, out var matrix)) {
            return matrix.GetDataSpan<int>();
        }
        
        return ReadOnlySpan<int>.Empty;
    }

    public override string ToString()
    {
        var sb = new StringBuilder("[");

        for (int i = 0; i < Count; i++) {
            if (i != 0) sb.Append(", ");
            sb.Append(this[i].ToString());
        }
        return sb.Append(']').ToString();
    }
    
}

public readonly struct FrameSideData
{
    public Handle<AVFrameSideData> Handle {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            unsafe {
                return new Handle<AVFrameSideData>(_handle, new SkipValidation());
            }
        }
    }

    public Span<byte> Data {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            unsafe {
                return new Span<byte>(_handle->data, (int)_handle->size);
            }
        }
    }
    
    public AVFrameSideDataType Type {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Handle.Ref.type;
    }

    public ObservedMediaDictionary Metadata {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            unsafe {
                return new ObservedMediaDictionary(&Handle.Raw->metadata);
            }
        }
    }
    
    internal readonly unsafe AVFrameSideData* _handle;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public FrameSideData(Handle<AVFrameSideData> handle)
    {
        unsafe {
            _handle = handle;
        }
    }

    /// <summary>
    /// Returns the side data payload reinterpreted as a <typeparamref name="T"/> pointer, 
    /// or null if the payload is smaller than <c>sizeof(T)</c>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<T> GetDataSpan<T>() where T : unmanaged
    {
        unsafe {
            var handle = Handle.Raw;
            if (_handle->size % (nuint)sizeof(T) is 0) {
                return new ReadOnlySpan<T>(handle->data, (int)handle->size);
            }
            
            return ReadOnlySpan<T>.Empty;
        }
    }
    

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override string ToString()
    {
        unsafe {
            return $"{FFHelper.PtrToStringUtf8(av_frame_side_data_name(Type))}: {_handle->size} bytes";
        }
    }
}
