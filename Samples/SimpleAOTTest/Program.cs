using FFmpegBindings.Abstractions;
using FFmpegBindings.Linked;


FFmpegLinked.Init();

unsafe {
    var ok = FFmpeg.av_buffer_alloc(2555);
    Console.WriteLine($"BUFFER with size {ok->size} | ptr {(IntPtr)ok->buffer}");
}