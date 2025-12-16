using System.Diagnostics;
using System.Text;

using FFmpegWrapper.Codecs.Decoding;
using FFmpegWrapper.Core;
using FFmpegWrapper.Hardware;
using FFmpegWrapper.Media;
using FFmpegWrapper.Media.Frames;
using FFmpegWrapper.Media.Packets;

using FFmpegBindings.Abstractions;

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
            if (Init != null)
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
                    frame.Save(filePath);
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
    public IReadOnlyList<AVImage> SampleImages(string pathToVideo, string fileName, byte count = 3,
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
        using var frame = new VideoFrame();

            
        if (!demuxer.TryFindBestStream(AVMediaType.AVMEDIA_TYPE_VIDEO, out var stream))
        {
            return Array.Empty<AVImage>();
        }
            
        using var decoder = (VideoDecoder)demuxer.CreateStreamDecoder(stream.Handle, false);
        
        if (HardwareDevice.TryCreateCompatibleHardwareDevice(
                decoder.Codec.Ref.id, stream.CodecPars.PictureFormat,
                out var hwDevice,
                out var hwConfig))
        {
            Console.WriteLine($"Using HW DEVICE: {hwDevice.Type}");
            decoder.SetupHardwareAccelerator(hwConfig, hwDevice);
        }
            
        decoder.SetThreadCount(0, true);
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
            var curtime = startTime + (interval * i);
            Console.WriteLine($"current time: {curtime}");
            if(demuxer.Seek(curtime, AVSEEK_FLAGS.AVSEEK_FLAG_BACKWARD, stream))
            {
                decoder.Flush();
            } 
            while (demuxer.Read(packet.Handle))
            {
                if (packet.StreamIndex != stream.Index) continue; //Ignore packets from other streams

                if (decoder.TrySendPacket(packet.Handle) is LavResult.Success)
                {
                    if (decoder.ReceiveFrame(frame.Handle))
                    {
                        var imagePath = $"{filePath}{i}.jpg";
                        frame.Save(imagePath);
                        images[i] = new AVImage(imagePath, frame.PixelFormat);
                        break;
                    }
                }

            }
        }
            
        return images;
    }

}