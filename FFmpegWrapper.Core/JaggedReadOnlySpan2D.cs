namespace FFmpegWrapper.Core;


/// <summary>
/// Read-only version of Span2D.
/// </summary>
public readonly ref struct JaggedReadOnlySpan2D<T> where T : unmanaged
{
    private readonly unsafe T** _rows;
    
    public readonly int Width;
    public readonly int Height;

    public unsafe JaggedReadOnlySpan2D(T** rows, int width, int height)
    {
        if (width < 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height < 0) throw new ArgumentOutOfRangeException(nameof(height));
        
        _rows = rows;
        Width = width;
        Height = height;
    }

    public unsafe JaggedReadOnlySpan2D(void** rows, int width, int height)
        : this((T**)rows, width, height)
    {
    }

    public int Length => Width * Height;
    public bool IsEmpty => Width == 0 || Height == 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe ReadOnlySpan<T> GetRowSpan(int row)
    {
        if ((uint)row >= (uint)Height)
            throw new ArgumentOutOfRangeException(nameof(row));
        
        return new ReadOnlySpan<T>(_rows[row], Width);
    }

    public ReadOnlySpan<T> this[int row]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => GetRowSpan(row);
    }
    
    public unsafe ref readonly T this[int row, int col]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if ((uint)row >= (uint)Height)
                throw new ArgumentOutOfRangeException(nameof(row));
            if ((uint)col >= (uint)Width)
                throw new ArgumentOutOfRangeException(nameof(col));
            
            return ref _rows[row][col];
        }
    }

    public void CopyTo(Span<T> destination)
    {
        if (destination.Length < Length)
            throw new ArgumentException("Destination too small", nameof(destination));

        int offset = 0;
        for (int row = 0; row < Height; row++)
        {
            GetRowSpan(row).CopyTo(destination.Slice(offset, Width));
            offset += Width;
        }
    }
}