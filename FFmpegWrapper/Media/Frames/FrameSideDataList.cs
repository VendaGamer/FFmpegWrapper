namespace FFmpegWrapper.Media.Frames;

using System.Text;

public unsafe struct FrameSideDataList
{
    readonly AVFrame* _frame;

    public AVFrame* Frame => _frame;
    public int Count => _frame->nb_side_data;

    public FrameSideData this[int index] {
        get {
            if ((uint)index > (uint)Count) {
                throw new ArgumentOutOfRangeException();
            }
            return new(_frame->side_data[index]);
        }
    }

    public FrameSideDataList(AVFrame* frame)
    {
        _frame = frame;
    }

    /// <summary> Returns the side data entry for the given type, or null if not present. </summary>
    public FrameSideData? Get(AVFrameSideDataType type)
    {
        AVFrameSideData* entry = ffmpeg.av_frame_get_side_data(_frame, type);
        return entry != null ? new FrameSideData(entry) : null;
    }

    /// <summary> Allocates and adds a new a side data entry. </summary>
    public FrameSideData Add(AVFrameSideDataType type, int size)
    {
        var entry = ffmpeg.av_frame_new_side_data(_frame, type, (ulong)size);
        if (entry == null) {
            throw new OutOfMemoryException();
        }
        return new FrameSideData(entry);
    }

    public bool Remove(AVFrameSideDataType type)
    {
        int prevCount = Count;
        ffmpeg.av_frame_remove_side_data(_frame, type);
        return Count != prevCount;
    }

    public void Clear()
    {
        // https://github.com/FFmpeg/FFmpeg/blob/4e120fbbbd087c3acbad6ce2e8c7b1262a5c8632/libavfilter/f_sidedata.c#L117
        while (_frame->nb_side_data != 0) {
            ffmpeg.av_frame_remove_side_data(_frame, _frame->side_data[0]->type);
        }
    }

    /// <summary> Returns the value of an <see cref="AVFrameSideDataType.AV_FRAME_DATA_DISPLAYMATRIX"/> entry. </summary>
    public int[]? GetDisplayMatrix()
    {
        var entry = Get(AVFrameSideDataType.AV_FRAME_DATA_DISPLAYMATRIX);
        
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
                return new Span<byte>(_handle->data, checked((int)_handle->size));
            }
        }
    }

    public MediaDictionary Metadata {
        get {
            unsafe
            {
                return new(Handle.Ref.metadata);
            }
        }
    }

    public AVFrameSideDataType Type => Handle->type;

    /// <summary>
    /// Returns the side data payload reinterpreted as a <typeparamref name="T"/> pointer, 
    /// or null if the payload is smaller than <c>sizeof(T)</c>.
    /// </summary>
    public FFHandle<T> GetDataHandle<T>() where T : unmanaged
    {
        unsafe {
            return _handle->size < (ulong)sizeof(T) ? null : (T*)_handle->data;
        }
    }
    

    public override string ToString()
    {
        unsafe
        {
            return $"{ffmpeg.av_frame_side_data_name(Type)}: {_handle->size} bytes";
        }
    }
}