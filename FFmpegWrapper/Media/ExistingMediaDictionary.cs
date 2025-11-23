namespace FFmpegWrapper.Media;

public readonly struct ExistingMediaDictionary : IFFHandleSourceObserver<AVDictionary>
{
    public FFHandleSource<AVDictionary> HandleSource { get; }


    internal readonly unsafe AVDictionary** _handleSource;
    
    public ExistingMediaDictionary(FFHandleSource<AVDictionary> handleSource)
    {
        unsafe {
            _handleSource = handleSource;
        }
    }
    

    public void Clear()
    {
        unsafe {
            ffmpeg.av_dict_free(_handleSource);
            ffmpeg.av_dict_copy(_handleSource, null, 0).CheckError("Could not clear allocate new dictionary");
        }
    }
}