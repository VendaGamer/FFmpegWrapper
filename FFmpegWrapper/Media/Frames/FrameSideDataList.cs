namespace FFmpegWrapper.Media.Frames;

using System.Text;

public struct FrameSideDataList : IFFHandleObserver<AVFrame>
{
    public FFHandle<AVFrame> Handle {
        get {
            unsafe {
                return _handle;
            }
        }
    }

    internal unsafe AVFrame* _handle;
    public int Count {
        get {
            unsafe
            {
                return Handle.Raw->nb_side_data;
            }
        }
    }

    public FrameSideData this[int index] {
        get {
            if (index > Count || index < 0) {
                throw new ArgumentOutOfRangeException();
            }

            unsafe {
                return new FrameSideData(Handle.Raw->side_data[index]);
            }
        }
    }

    public FrameSideDataList(FFHandle<AVFrame> handle)
    {
        unsafe
        {
            _handle = handle;
        }
    }

    /// <summary> Returns the side data entry for the given type, or null if not present. </summary>
    public FrameSideData? Get(AVFrameSideDataType type)
    {
        unsafe
        {
            AVFrameSideData* entry = av_frame_get_side_data(Handle, type);
            return entry != null ? new FrameSideData(entry) : null;
        }
    }

    /// <summary> Allocates and adds a new a side data entry. </summary>
    public FrameSideData Add(AVFrameSideDataType type, nuint size)
    {
        unsafe
        {
            var entry = av_frame_new_side_data(Handle, type, size);
            if (entry == null) {
                throw new OutOfMemoryException();
            }
            return new FrameSideData(entry);
        }
    }
    
    public bool Remove(AVFrameSideDataType type)
    {
        unsafe
        {
            int prevCount = Count;
            av_frame_remove_side_data(Handle, type);
            return Count != prevCount;
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
        var martrix = Get(AVFrameSideDataType.AV_FRAME_DATA_DISPLAYMATRIX);

        if (martrix is not null) {
            return martrix.Value.GetDataSpan<int>();
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

public struct FrameSideData(FFHandle<AVFrameSideData> handle)
{
    public FFHandle<AVFrameSideData> Handle {
        get {
            unsafe {
                return _handle;
            }
        }
    }
    
    private unsafe AVFrameSideData* _handle;

    public Span<byte> Data {
        get {
            unsafe
            {
                return new Span<byte>(_handle->data, (int)_handle->size);
            }
        }
    }

    public MediaDictionary Metadata {
        get {
            unsafe
            {
                return new MediaDictionary(Handle.Ref.metadata);
            }
        }
    }

    public AVFrameSideDataType Type => Handle.Ref.type;

    /// <summary>
    /// Returns the side data payload reinterpreted as a <typeparamref name="T"/> pointer, 
    /// or null if the payload is smaller than <c>sizeof(T)</c>.
    /// </summary>
    public ReadOnlySpan<T> GetDataSpan<T>() where T : unmanaged
    {
        unsafe {
            if (_handle->size % (nuint)sizeof(T) is 0) {
                return new ReadOnlySpan<T>(handle.Raw->data, (int)handle.Raw->size);
            }

            return ReadOnlySpan<T>.Empty;
        }
    }
    

    public override string ToString()
    {
        unsafe {
            return $"{FFHelper.PtrToStringUtf8(av_frame_side_data_name(Type))}: {_handle->size} bytes";
        }
    }
}
