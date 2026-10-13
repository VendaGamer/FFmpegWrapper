namespace FFmpegWrapper.Tests;

using Media;

public class DictionaryTests
{
    [Test]
    public async Task DictTest()
    {
        var dictionaryOwner = new MediaDictionaryOwner();
        var dict = dictionaryOwner.Dictionary;

        dict["FileName"u8] = "test.mp4"u8;
        dict.SetIntValue("FrameRate"u8, 60);

        await Assert.That(dict["FileName"u8].SequenceEqual("test.mp4"u8)).IsTrue();
        dictionaryOwner.Dispose();

        Assert.Throws<ObjectDisposedException>(() => _ = dictionaryOwner.Dictionary);
    }
    
    [Test]
    public async Task DictParseTest()
    {
        var dictionaryOwner = MediaDictionaryOwner.Parse("FileName:test.mp4|Author:Vencaa"u8);
        
        await Assert.That(dictionaryOwner.Dictionary.Count).IsEqualTo(2);
        await Assert.That(dictionaryOwner.Dictionary["FileName"u8].SequenceEqual("test.mp4"u8)).IsTrue();
        await Assert.That(dictionaryOwner.Dictionary.GetValue("Author"u8).SequenceEqual("Vencaa"u8)).IsTrue();
        
        dictionaryOwner.Dictionary.Clear();
        dictionaryOwner.Dispose();
        Assert.Throws<ObjectDisposedException>(() => _ = dictionaryOwner.Dictionary);
    }
}