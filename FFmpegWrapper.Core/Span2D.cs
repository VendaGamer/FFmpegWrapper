namespace FFmpegWrapper.Core;

public readonly ref struct Span2D<T>
{
    private readonly unsafe void** _handle;

    public readonly int RowLength;
    public readonly int ColLength;

    public unsafe Span2D(void** handle, int rows, int cols)
    {
        _handle = handle;
        RowLength = rows;
        ColLength = cols;
    }
    
    public Span<T> this[int row] {
        get {
            unsafe {
                return new Span<T>(_handle[row], RowLength);
            }
        }
    }

    public ref T this[int row, int col] {
        get {
            return ref this[row][col];
        }
    }
    
    
    
}