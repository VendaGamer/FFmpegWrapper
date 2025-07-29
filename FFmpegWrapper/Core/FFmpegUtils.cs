namespace FFmpegWrapper.Core;

using System.Buffers;
using System.Text;

public static unsafe class FFmpegUtils
{
    private static av_log_set_callback_callback logCallback = null!;
    private static Action<FFmpegLogLevel, string> userCallback = null!;
    private static FFmpegLogLevel s_MinLevel;
    
    private const int DefaultBufferSize = 1024;
    
    // Thread-local storage for print prefix to avoid race conditions
    [ThreadStatic]
    private static int t_PrintPrefix = 1;

    /// <summary> Set log level and message callback. </summary>
    /// <param name="minLevel">Level of logging</param>
    /// <param name="cb"> Callback that will receive log messages. If null, will default printing to stdout. </param> 
    public static void SetLoggerCallback(FFmpegLogLevel minLevel, Action<FFmpegLogLevel, string> cb)
    {
        s_MinLevel = minLevel;
        userCallback = cb;
        logCallback = NativeCb;
        
        ffmpeg.av_log_set_level((int)minLevel);
        ffmpeg.av_log_set_callback(logCallback);
        
        return;

        void NativeCb(void* avcl, int level, string fmt, byte* vl)
        {
            if (level > (int)s_MinLevel) return;
            
            const int stackAllocThreshold = 512;
            
            int estimatedLength = EstimateLogLength(fmt);
            
            if (estimatedLength <= stackAllocThreshold)
            {
                byte* stackBuffer = stackalloc byte[stackAllocThreshold];
                ProcessLogMessageStack(avcl, level, fmt, vl, stackBuffer, stackAllocThreshold);
            }
            else
            {
                // Use pooled buffer for larger messages
                byte[] pooledBuffer = ArrayPool<byte>.Shared.Rent(Math.Max(estimatedLength, DefaultBufferSize));
                try
                {
                    ProcessLogMessage(avcl, level, fmt, vl, pooledBuffer);
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(pooledBuffer);
                }
            }
        }

        void ProcessLogMessage(void* avcl, int level, string fmt, byte* vl, byte[] buffer)
        {
            int length;
            
            fixed (byte* pBuffer = buffer)
            {
                int localPrintPrefix = t_PrintPrefix;
                length = ffmpeg.av_log_format_line2(avcl, level, fmt, vl, pBuffer, buffer.Length, &localPrintPrefix);
                length = Math.Min(length, buffer.Length - 1);
                t_PrintPrefix = localPrintPrefix;
            }

            if (length > 0)
            {
                if (buffer[length - 1] == '\n') 
                    length--;
                
                string message = Encoding.UTF8.GetString(buffer, 0, length);
                userCallback!((FFmpegLogLevel)level, message);
            }
        }
        
        void ProcessLogMessageStack(void* avcl, int level, string fmt,
            byte* vl, byte* buffer, int bufferLength)
        {
            int localPrintPrefix = t_PrintPrefix;
            int length = ffmpeg.av_log_format_line2(avcl, level, fmt, vl, buffer, bufferLength, &localPrintPrefix);
            length = Math.Min(length, bufferLength - 1);
            t_PrintPrefix = localPrintPrefix;

            if (length > 0)
            {
                if (buffer[length - 1] == '\n')
                    length--;
                
                string message = Encoding.UTF8.GetString(buffer, length);
                userCallback!((FFmpegLogLevel)level, message);
                
            }
        }

        static int EstimateLogLength(string fmt)
        {
            return fmt.Length * 2 + 256;
        }
    }
}