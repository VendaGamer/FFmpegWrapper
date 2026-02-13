namespace FFmpegWrapper.Core;

using System.Buffers;
using System.Runtime.InteropServices;
using System.Text;

public static class FFmpegUtils
{
    private static av_log_set_callback_callback logCallback = null!;
    private static Action<AVLog, string> userCallback = null!;
    private static AVLog s_MinLevel;
    
    
    private const int DefaultBufferSize = 1024;
    
    [ThreadStatic] 
    private static int s_tPrintPrefix;

    /// <summary> Set log level and message callback. </summary>
    /// <param name="minLevel">Level of logging</param>
    /// <param name="cb"> Callback that will receive log messages. If null, will default printing to stdout. </param> 
    public static void SetLoggerCallback(AVLog minLevel, Action<AVLog, string> cb)
    {
        unsafe
        {
            s_MinLevel = minLevel;
            userCallback = cb;
            logCallback = NativeCb;
        
            av_log_set_level((int)minLevel);
            
            av_log_set_callback((delegate* unmanaged[Cdecl]<void*, int, byte*, void*, void>)
                Marshal.GetFunctionPointerForDelegate(logCallback));
        
            return;

            static void NativeCb(void* avcl, int level, byte* fmt, byte* vl)
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

            static void ProcessLogMessage(void* avcl, int level, byte* fmt, byte* vl, byte[] buffer)
            {
                int length;
            
                fixed (byte* pBuffer = buffer)
                {
                    int localPrintPrefix = s_tPrintPrefix;
                    length = av_log_format_line2(avcl, level, fmt, vl, pBuffer, buffer.Length, &localPrintPrefix);
                    length = Math.Min(length, buffer.Length - 1);
                    s_tPrintPrefix = localPrintPrefix;
                }

                if (length > 0)
                {
                    if (buffer[length - 1] == '\n') 
                        length--;
                
                    string message = Encoding.UTF8.GetString(buffer, 0, length);
                    userCallback!((AVLog)level, message);
                }
            }
        
            static void ProcessLogMessageStack(void* avcl, int level, byte* fmt, byte* vl, byte* buffer, int bufferLength)
            {
                int localPrintPrefix = s_tPrintPrefix;
                int length = av_log_format_line2(avcl, level, fmt, vl, buffer, bufferLength, &localPrintPrefix);
                length = Math.Min(length, bufferLength - 1);
                s_tPrintPrefix = localPrintPrefix;

                if (length > 0)
                {
                    if (buffer[length - 1] == '\n')
                        length--;
                
                    string message = Encoding.UTF8.GetString(buffer, length);
                    userCallback!((AVLog)level, message);
                
                }
            }

            static int EstimateLogLength(byte* fmt)
            {
                var span = FFHelper.GetSpanFromSentinelTerminatedPtr<byte>(fmt,0);
                return span.Length * 2 + 256;
            }
        }
    }
}
