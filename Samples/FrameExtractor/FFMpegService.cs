using System.Diagnostics;
using System.Text;

using FFmpegBindings.Abstractions;

using FFmpegWrapper.Codecs.Decoding;
using FFmpegWrapper.Core;
using FFmpegWrapper.Hardware;
using FFmpegWrapper.Media;
using FFmpegWrapper.Media.Formats;
using FFmpegWrapper.Media.Frames;
using FFmpegWrapper.Media.Packets;

namespace FrameExtractor;

using FFmpegWrapper.Codecs;
using FFmpegWrapper.Codecs.Encoding;
using FFmpegWrapper.Containers;
using FFmpegWrapper.Processing;

/// <summary>
/// Helper class for working with FFmpeg.Wrapper
/// </summary>
public sealed class FFMpegService
{
    private static bool isInitialized = false;
    public static Action? Init { get; set; }
    
    private static readonly Dictionary<AVLog, ConsoleColor> logColors = new()
    {
        { AVLog.AV_LOG_DEBUG, ConsoleColor.Cyan},
        { AVLog.AV_LOG_INFO, ConsoleColor.White},
        { AVLog.AV_LOG_VERBOSE, ConsoleColor.Gray},
        { AVLog.AV_LOG_WARNING, ConsoleColor.Yellow},
        { AVLog.AV_LOG_ERROR, ConsoleColor.Red},
        { AVLog.AV_LOG_FATAL, ConsoleColor.DarkRed},
        { AVLog.AV_LOG_PANIC, ConsoleColor.DarkMagenta},
        { AVLog.AV_LOG_TRACE , ConsoleColor.DarkGreen},
    };
    public FFMpegService()
    {
        if (!isInitialized)
        {
            if (Init is not null)
            {
                Init();
                isInitialized = true;
            }
        }
    }
    /// <summary>
    ///  Saves Image from video
    /// </summary>
    /// <param name="pathToVideo">Directory path or url to video</param>
    /// <param name="timestamp"></param>
    /// <param name="fileName"></param>
    /// <returns>Image info</returns>
    public AVImage? SampleImage(string pathToVideo, TimeSpan timestamp, string fileName)
    {
        var filePath = 
            Path.IsPathRooted(fileName) ?
                Path.GetFullPath(fileName) :
                Path.Combine("./", fileName);
            
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
            
        using var demuxer = new MediaDemuxer(Encoding.UTF8.GetBytes(pathToVideo));
        using var packet = new MediaPacket();
        using var frame = new VideoFrame();

        if (!demuxer.TryFindBestStream(AVMediaType.AVMEDIA_TYPE_VIDEO, out var stream))
        {
            stream = demuxer.Streams[0];
        }
            
        using var decoder = (VideoDecoder)demuxer.CreateStreamDecoder(stream.Handle,false);
        
        demuxer.Seek(timestamp, AVSEEK_FLAGS.AVSEEK_FLAG_BACKWARD, stream);
        decoder.Open();

        while (demuxer.Read(packet.Handle))
        {
            if (packet.StreamIndex != stream.Index) continue; //Ignore packets from other streams

            if (decoder.TrySendPacket(packet.Handle) is LavResult.Success)
            {
                if (decoder.ReceiveFrame(frame.Handle))
                {
                    frame.Save(filePath, new PictureFormat(0,0, AVPixelFormat.AV_PIX_FMT_YUV420P));
                    return new AVImage(filePath, frame.PixelFormat);
                }
            }
            else
            {
                decoder.Flush();
            }
        }
        return null;
    }

    public AVImage? SampleImage(Uri mediaUri, TimeSpan timestamp, string fileName)
    {
        return SampleImage(mediaUri.AbsoluteUri,timestamp,fileName);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="pathToVideo">Directory path or url to video</param>
    /// <param name="fileName"> name or path and name</param>
    /// <param name="startEndCrop">
    ///     Usually videos start and end with black image so this is used to prevent from sampling these
    /// </param>
    /// <param name="count"></param>
    /// <assertions>
    /// do not use smaller than 2
    /// </assertions>
    /// <returns></returns>
    public IReadOnlyList<AVImage> SampleImages(string pathToVideo, string fileName, string fileExtension, byte count = 3,
    TimeSpan? startEndCrop = null)
    {
        Debug.Assert(count > 1, "Please use SampleImage instead of SampleImages when sampling 1 image");

        var filePath = 
            Path.IsPathRooted(fileName) ?
                Path.GetFullPath(fileName) :
                Path.Combine("./", fileName);
            
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
            
        using var demuxer = new MediaDemuxer(Encoding.UTF8.GetBytes(pathToVideo));
        using var packet = new MediaPacket();
        using var decFrame = new VideoFrame();

            
        if (!demuxer.TryFindBestStream(AVMediaType.AVMEDIA_TYPE_VIDEO, out var stream))
        {
            return Array.Empty<AVImage>();
        }
            
        using var decoder = (VideoDecoder)demuxer.CreateStreamDecoder(stream.Handle, false);
        
        var outputFormat = OutputFormat.FindByExtension(Encoding.UTF8.GetBytes($"dummy.{fileExtension}"));
        var outCodec = MediaCodec.GetEncoder(outputFormat.VideoCodec);
        
        var outFormat = new PictureFormat(decoder.Width, decoder.Height,
            outCodec.GetBestPixelFormat(decoder.PixelFormat));
        
        using var encoder = new VideoEncoder(outCodec, outFormat, decoder.FrameRate);
        using var encFrame = new VideoFrame(outFormat);
        
        using var sws = new SwScaler(decoder.FrameFormat, outFormat);
        sws.SetColorspace(decoder.Colorspace, encoder.Colorspace);
        
        // Configure encoder for specific formats
        encoder.Handle.Ref.strict_std_compliance = (int)FFCompliance.FF_COMPLIANCE_UNOFFICIAL;
        encoder.GlobalQuality = 32 * FFmpegConstants.FF_QP2LAMBDA;
        encoder.SetThreadCount(0, true);
        decoder.SetThreadCount(0, true);
        
        encoder.Open();
        decoder.Open();
        
        var startTime = stream.GetTimestamp(stream.StartTime ?? 0);
        var endTime = stream.Duration ?? TimeSpan.FromHours(2);

        if (startEndCrop.HasValue)
        {
            startTime += startEndCrop.Value;
            endTime -= startEndCrop.Value;
        }

        var interval = endTime - startTime;
        interval /= (count-1);

        var images = new AVImage[count];
        
        for (byte i = 0; i < count; i++)
        {
            var curTime = startTime + (interval * i);
            Console.WriteLine($"current time: {curTime}");
            
            var outputPath = $"{filePath}{i}.{fileExtension}";
            
            if(demuxer.Seek(curTime, AVSEEK_FLAGS.AVSEEK_FLAG_BACKWARD, stream))
            {
                decoder.Flush();
            } 
            
            while (demuxer.Read(packet.Handle))
            {
                if (packet.StreamIndex != stream.Index)
                    continue;

                if (decoder.TrySendPacket(packet.Handle) is not LavResult.Success)
                    continue;

                if (!decoder.ReceiveFrame(decFrame.Handle))
                    continue;
                
                // Convert pixel format
                sws.Convert(decFrame.Handle, encFrame.Handle);
                using var muxer = new MediaMuxer(Encoding.UTF8.GetBytes(outputPath));
                muxer.AddStream(encoder);
                muxer.EncodeAndWrite(stream, encoder, encFrame.Handle);
                images[i] = new AVImage(outputPath, outFormat.PixelFormat);
                break;
            }
        }
            
        return images;
    }

}