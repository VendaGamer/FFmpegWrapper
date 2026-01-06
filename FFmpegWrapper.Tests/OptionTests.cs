namespace FFmpegWrapper.Tests;

public class OptionTests: TestBase
{
    [Fact]
    public void GetOptions()
    {
        var options = MediaCodec.GetEncoder("libx264"u8).GetOptions(removeAliases: false);
        Assert.NotEmpty(options);

        var mestOpt = options.First(o => o.Name.SequenceEqual("me_method"u8));
        var namedOpts = mestOpt.GetNamedValues();

        Assert.Equal("me_method: int", mestOpt.ToString());
        Assert.Equal("Set motion estimation method"u8, mestOpt.Description);
        Assert.Contains(namedOpts, v => v.Name.SequenceEqual("hex"u8));
        Assert.DoesNotContain(namedOpts, v => v.Type != AVOptionType.AV_OPT_TYPE_CONST);
    }
}