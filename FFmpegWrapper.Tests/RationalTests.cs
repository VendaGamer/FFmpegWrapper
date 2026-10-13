namespace FFmpegWrapper.Tests;

using Core;

public class RationalTests
{
    [Test]
    public async Task Casting()
    {
        var orig = new AVRational { num = 123, den = 456 };

        await Assert.That((Rational)orig).IsEqualTo(new Rational(123, 456));
        await Assert.That((AVRational)(Rational)orig).IsEqualTo(orig);
        await Assert.That(123).IsEqualTo(new Rational(123, 1));

        await Assert.That((double)new Rational(1, 8)).IsEqualTo(0.125);
        await Assert.That(Rational.FromDouble(3.14159, 100)).IsEqualTo(new Rational(22, 7));
    }

    [Test]
    public async Task Rescale()
    {
        await Assert.That(Rational.Rescale(123456, new Rational(1, 100), new Rational(1, 1))).IsEqualTo(1235);
        await Assert.That(Rational.GetTimeSpan(123456, new Rational(1, 1000))).IsEqualTo(TimeSpan.FromMilliseconds(123456));
    }

    [Test]
    public async Task Arithmetic()
    {
        var half = new Rational(1, 2);
        var quart = new Rational(1, 4);
        var negQuart = new Rational(-1, 4);

        await Assert.That(half + half).IsEqualTo(new Rational(1, 1));
        await Assert.That(half - half).IsEqualTo(new Rational(0, 1));
        await Assert.That(half * half).IsEqualTo(new Rational(1, 4));
        await Assert.That(half / quart).IsEqualTo(new Rational(2, 1));

        await Assert.That(half + negQuart).IsEqualTo(new Rational(1, 4));
        await Assert.That(half - negQuart).IsEqualTo(new Rational(3, 4));
        await Assert.That(half * negQuart).IsEqualTo(new Rational(-1, 8));
        await Assert.That(half / negQuart).IsEqualTo(new Rational(-2, 1));
    }

    [Test]
    public async Task Comparison()
    {
        await Assert.That(new Rational(5, 10).CompareTo(new Rational(1, 2))).IsEqualTo(0);
        await Assert.That(new Rational(5, 1).CompareTo(new Rational(10, 2))).IsEqualTo(0);

        await Assert.That(new Rational(5, 1).CompareTo(new Rational(10, 1))).IsEqualTo(-1);
        await Assert.That(new Rational(10, 1).CompareTo(new Rational(5, 1))).IsEqualTo(1);

        await Assert.That(new Rational(1, 4).CompareTo(new Rational(1, 2))).IsEqualTo(-1);
        await Assert.That(new Rational(1, 2).CompareTo(new Rational(1, 4))).IsEqualTo(1);

        await Assert.That(new Rational(123, 0).CompareTo(new Rational(456, 0))).IsEqualTo(0);
        await Assert.That(new Rational(0, 0).CompareTo(new Rational(456, 0))).IsEqualTo(int.MinValue);

        await Assert.That(new Rational(0, 0).Equals(new Rational(0, 0))).IsTrue(); // NaN
        await Assert.That(new Rational(123, 0).Equals(new Rational(456, 0))).IsTrue(); // Inf
        await Assert.That(new Rational(-123, 0).Equals(new Rational(456, 0))).IsFalse(); // -Inf
        await Assert.That(new Rational(123, 0).Equals(new Rational(-456, 0))).IsFalse(); // -Inf
    }
}