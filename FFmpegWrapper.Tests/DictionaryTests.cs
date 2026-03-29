namespace FFmpegWrapper.Tests;

using Media;

public class DictionaryTests : TestBase
{
    [Fact]
    public void DictTest()
    {
        var dictionaryOwner = new MediaDictionaryOwner();
        var dict = dictionaryOwner.Dictionary;

        dict["FileName"u8] = "test.mp4"u8;
        dict.SetIntValue("FrameRate"u8, 60);
        
        Assert.Equal("test.mp4"u8, dict["FileName"u8]);
        dictionaryOwner.Dispose();

        Assert.Throws<ObjectDisposedException>(() => dictionaryOwner.Dictionary);
    }
    
    [Fact]
    public void DictParseTest()
    {
        var dictionaryOwner = MediaDictionaryOwner.Parse("FileName:test.mp4|Author:Vencaa"u8);
        var dict = dictionaryOwner.Dictionary;
        
        Assert.Equal(2, dict.Count);
        Assert.Equal("test.mp4"u8, dict["FileName"u8]);
        Assert.Equal("Vencaa"u8, dict.GetValue("Author"u8));
        
        dict.Clear();
        dictionaryOwner.Dispose();
        Assert.Throws<ObjectDisposedException>(() => dictionaryOwner.Dictionary);
    }
    
}