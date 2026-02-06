namespace FFmpegWrapper.Core;

public readonly struct Resolution(int width, int height) : IEquatable<Resolution>
{
    public readonly int Width = width;
    public readonly int Height = height;
    
    public static Resolution Zero => new(0, 0);
    public static Resolution Max => new(int.MaxValue, int.MaxValue);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Resolution ScaleUp(int factor) => new(Width * factor, Height * factor);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(Resolution other) => Width == other.Width && Height == other.Height;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool Equals(object? obj) => obj is Resolution other && Equals(other);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode() => HashCode.Combine(Width, Height);
}