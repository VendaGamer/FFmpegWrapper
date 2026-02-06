namespace FFmpegWrapper.Core;

/// <summary> Represents a rational number (pair of numerator and denominator). </summary>
/// <remarks>
/// While rational numbers can be expressed as floating-point numbers, the
/// conversion process is a lossy one, so are floating-point operations. On the
/// other hand, the nature of FFmpeg demands highly accurate calculation of
/// timestamps. This struct serves as a generic interface for manipulating 
/// rational numbers as pairs of numerators and denominators.
/// </remarks>
public readonly struct Rational(int num, int den)
    : IEquatable<Rational>, IComparable<Rational>, IEqualityComparer<Rational>
{
    public readonly int Num = num;
    public readonly int Den = den;
    
    public static Rational Zero => new(0, 1);
    public static Rational One => new(1, 1);
    
    public static readonly Rational MaxValue = new(int.MaxValue, int.MaxValue);
    public static readonly Rational MinValue = new(int.MinValue, int.MinValue);
    public bool IsValidFrameRate => Num > 0 && Den > 0;

    /// <summary> Returns the reciprocal of this rational, <c>1/q</c>. </summary>
    public Rational Reciprocal() => new(Den, Num);

    /// <param name="max"> Maximum allowed numerator and denominator. </param>
    public static Rational FromDouble(double value, int max) => av_d2q(value, max);

    /// <summary> Rescales a fixed-point integer based on <paramref name="oldScale"/> to <paramref name="newScale"/>. </summary>
    public static long Rescale(long value, Rational oldScale, Rational newScale) => av_rescale_q(value, oldScale, newScale);

    /// <summary> Rescales a timestamp based around an arbitrary time scale to a <see cref="TimeSpan"/>. </summary>
    /// <param name="scale">The scale that represents one second of time.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TimeSpan GetTimeSpan(long timestamp, Rational scale)
    {
        long ticks = Rescale(timestamp, scale, new Rational(1, (int)TimeSpan.TicksPerSecond));
        return TimeSpan.FromTicks(ticks);
    }

    // Optimized arithmetic operators - inline and call FFmpeg functions directly
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Rational operator +(Rational a, Rational b) => av_add_q(a, b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Rational operator -(Rational a, Rational b) => av_sub_q(a, b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Rational operator *(Rational a, Rational b) => av_mul_q(a, b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Rational operator /(Rational a, Rational b) => av_div_q(a, b);

    // Optimized comparison operators - single comparison result reused
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(Rational a, Rational b)
    {
        // Fast path: check if numerators and denominators are equal
        if (a.Num == b.Num && a.Den == b.Den)
            return true;
        
        // Handle degenerate cases (0/0) and proper comparison
        return a.CompareTo(b) is 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(Rational a, Rational b) => !(a == b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator <(Rational a, Rational b)
    {
        long tmp = a.Num * (long)b.Den - b.Num * (long)a.Den;
        
        if (tmp != 0)
            return (int)((tmp ^ a.Den ^ b.Den) >> 63) is -1;
        
        return a.CompareTo(b) is -1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator >(Rational a, Rational b) => b < a;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator <=(Rational a, Rational b) => !(a > b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator >=(Rational a, Rational b) => !(a < b);

    // Optimized cast operators
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator double(Rational q) => q.Num / (double)q.Den;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Rational(int num) => new(1, num);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Rational(AVRational q) => new(q.num, q.den);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator AVRational(Rational q) => new() { num = q.Num, den = q.Den };

    public override string ToString() => $"{Num}/{Den}";
    public override bool Equals(object? other) => other is Rational r && Equals(r);

    /// <inheritdoc />
    public override int GetHashCode() => Math.Round((double)this, 10).GetHashCode();

    /// <inheritdoc />
    public bool Equals(Rational other) => other == this || ((other.Den | Den) == 0 && (other.Num ^ Num) >= 0);

    /// <inheritdoc />
    public int CompareTo(Rational other)
    {
        long tmp = Num * (long)other.Den - other.Num * (long)Den;
 
        if (tmp is not 0)
            return (int)((tmp ^ Den ^ other.Den) >> 63) | 1;
        if (other.Den is not 0 && Den is not 0)
            return 0;
        if (Num is not 0 && other.Num is not 0)
            return (Num >> 31) - (other.Num >> 31);

        return int.MinValue;
    }

    /// <inheritdoc />
    public bool Equals(Rational x, Rational y)
    {
        return x.Num == y.Num && x.Den == y.Den;
    }

    /// <inheritdoc />
    public int GetHashCode(Rational obj)
    {
        unchecked
        {
            return (obj.Num * 397) ^ obj.Den;
        }
    }
}
