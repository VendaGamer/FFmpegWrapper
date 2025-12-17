namespace FFmpegWrapper.Core;

using System;
using System.Runtime.CompilerServices;

/// <summary>
/// Provides a type-safe view over a 2D array stored as an array of pointers (T**).
/// Each row is a separate memory allocation, similar to FFmpeg's data structure.
/// </summary>
public readonly ref struct Span2D<T> where T : unmanaged
{
    private readonly unsafe T** _rows;
    
    public readonly int Width;
    public readonly int Height;

    /// <summary>
    /// Creates a new Span2D from a pointer to an array of row pointers.
    /// </summary>
    /// <param name="rows">Pointer to array of row pointers (T**)</param>
    /// <param name="width">Number of elements per row</param>
    /// <param name="height">Number of rows</param>
    public unsafe Span2D(T** rows, int width, int height)
    {
        if (width < 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height < 0) throw new ArgumentOutOfRangeException(nameof(height));

        string a;
        a.AsMemory()
        
        _rows = rows;
        Width = width;
        Height = height;
    }

    /// <summary>
    /// Alternative constructor accepting void** for interop scenarios.
    /// </summary>
    public unsafe Span2D(void** rows, int width, int height)
        : this((T**)rows, width, height)
    {
    }

    /// <summary>
    /// Gets the total number of elements (Width * Height).
    /// </summary>
    public int Length => Width * Height;

    /// <summary>
    /// Returns true if either dimension is zero.
    /// </summary>
    public bool IsEmpty => Width == 0 || Height == 0;

    /// <summary>
    /// Gets a Span representing the specified row.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe Span<T> GetRowSpan(int row)
    {
        if ((uint)row >= (uint)Height)
            throw new ArgumentOutOfRangeException(nameof(row));
        
        return new Span<T>(_rows[row], Width);
    }

    /// <summary>
    /// Indexer to access rows as Spans.
    /// </summary>
    public Span<T> this[int row]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => GetRowSpan(row);
    }

    /// <summary>
    /// Gets a reference to the element at the specified position.
    /// </summary>
    public unsafe ref T this[int row, int col]
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

    /// <summary>
    /// Attempts to get a reference to the element at the specified position without bounds checking.
    /// Use with caution - caller must ensure indices are valid.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe ref T GetRefUnchecked(int row, int col)
    {
        return ref _rows[row][col];
    }

    /// <summary>
    /// Tries to get a row span without throwing exceptions.
    /// </summary>
    public bool TryGetRowSpan(int row, out Span<T> span)
    {
        if ((uint)row >= (uint)Height)
        {
            span = default;
            return false;
        }

        unsafe
        {
            span = new Span<T>(_rows[row], Width);
        }
        return true;
    }

    /// <summary>
    /// Copies the entire 2D data to a contiguous destination span.
    /// Destination must have at least Width * Height elements.
    /// </summary>
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

    /// <summary>
    /// Fills all elements with the specified value.
    /// </summary>
    public void Fill(T value)
    {
        for (int row = 0; row < Height; row++)
        {
            GetRowSpan(row).Fill(value);
        }
    }

    /// <summary>
    /// Clears all elements to their default value.
    /// </summary>
    public void Clear()
    {
        for (int row = 0; row < Height; row++)
        {
            GetRowSpan(row).Clear();
        }
    }

    /// <summary>
    /// Creates a ReadOnlySpan2D view of this data.
    /// </summary>
    public unsafe ReadOnlySpan2D<T> AsReadOnly()
    {
        return new ReadOnlySpan2D<T>(_rows, Width, Height);
    }

    /// <summary>
    /// Gets a pointer to the specified row (advanced scenarios).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe T* GetRowPointer(int row)
    {
        if ((uint)row >= (uint)Height)
            throw new ArgumentOutOfRangeException(nameof(row));
        
        return _rows[row];
    }
}

/// <summary>
/// Read-only version of Span2D.
/// </summary>
public readonly ref struct ReadOnlySpan2D<T> where T : unmanaged
{
    private readonly unsafe T** _rows;
    
    public readonly int Width;
    public readonly int Height;

    public unsafe ReadOnlySpan2D(T** rows, int width, int height)
    {
        if (width < 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height < 0) throw new ArgumentOutOfRangeException(nameof(height));
        
        _rows = rows;
        Width = width;
        Height = height;
    }

    public unsafe ReadOnlySpan2D(void** rows, int width, int height)
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