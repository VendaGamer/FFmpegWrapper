using Avalonia.Controls;

namespace AvaloniaIntegration;

using System.Diagnostics;

using Avalonia.Threading;

using FFmpegBindings.Abstractions;
using FFmpegBindings.Linked;
using FFmpegWrapper.Codecs.Decoding;
using FFmpegWrapper.Core;
using FFmpegWrapper.Media;
using FFmpegWrapper.Media.Frames;
using FFmpegWrapper.Media.Packets;
using FFmpegWrapper.Media.Streams;
using SoundFlow.Abstracts.Devices;
using SoundFlow.Backends.MiniAudio;
using SoundFlow.Enums;
using SoundFlow.Structs;

public partial class MainWindow : Window
{
    private MediaDemuxer _demuxer;
    private VideoDecoder _decoder;
    private VideoFrame _currentFrame;
    private MediaPacket _packet;
    private MediaStream _videoStream;
    private MediaStream _audioStream;
    private DispatcherTimer _timer;
    
    private Stopwatch _playbackClock;
    private long? _startTimestamp;
    private double _timeBase;
    private bool _isFirstFrame = true;
    private Queue<VideoFrame> _frameQueue = new(MaxQueueSize);
    private AudioQueue _audioQueue;
    private const int MaxQueueSize = 50;

    private MiniAudioEngine _audioEngine;
    private AudioPlaybackDevice _playbackDevice;
    
    public unsafe MainWindow()
    {
        InitializeComponent();
        FFmpegLinked.Init();
        
        _audioEngine = new MiniAudioEngine();
        var defaultDevice = _audioEngine.PlaybackDevices[0];
        
        _demuxer = new MediaDemuxer("http://commondatastorage.googleapis.com/gtv-videos-bucket/sample/BigBuckBunny.mp4"u8);
        _packet = new MediaPacket();

        if (!_demuxer.TryFindBestStream(AVMediaType.AVMEDIA_TYPE_VIDEO, out _videoStream))
            throw new Exception("Could not find video stream");

        _decoder = (VideoDecoder)_demuxer.CreateStreamDecoder(_videoStream.Handle);
        _currentFrame = new VideoFrame(_decoder.FrameFormat);
        
        if (!_demuxer.TryFindBestStream(AVMediaType.AVMEDIA_TYPE_AUDIO, out _audioStream))
            throw new Exception("Could not find video stream");
        
        
        _playbackDevice = _audioEngine.InitializePlaybackDevice(defaultDevice);
        
        
        // Get time base for timestamp conversion
        _timeBase = _videoStream.TimeBase.Num / (double)_videoStream.TimeBase.Den;
        _audioQueue = new AudioQueue(_audioStream.CodecPars.AudioFormat, 50);
        
        // Start playback clock
        _playbackClock = Stopwatch.StartNew();
        
        // Use a high-frequency timer to check for frames
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(1) // Check frequently
        };
        _timer.Tick += OnTimerTick;
        _timer.Start();
        
        // Start decoding thread
        Task.Run(DecodeLoop);
    }

    private async Task DecodeLoop()
    {
        while (_demuxer.Read(_packet.Handle))
        {
            if (_packet.StreamIndex != _videoStream.Index)
                continue;

            if (_decoder.TrySendPacket(_packet.Handle) is not LavResult.Success)
            {
                _decoder.Flush();
                continue;
            }

            while (true)
            {
                var frame = new VideoFrame(_decoder.FrameFormat);
                
                if (!_decoder.ReceiveFrame(frame.Handle))
                {
                    frame.Dispose();
                    break;
                }

                // Wait if queue is full
                while (_frameQueue.Count >= MaxQueueSize)
                {
                    await Task.Delay(1);
                }

                lock (_frameQueue)
                {
                    _frameQueue.Enqueue(frame);
                }
            }
        }
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        if (_frameQueue.Count == 0)
            return;

        VideoFrame? frameToDisplay = null;
        
        lock (_frameQueue)
        {
            if (_frameQueue.Count == 0)
                return;

            var nextFrame = _frameQueue.Peek();
            
            if (_isFirstFrame)
            {
                // Display first frame immediately and set reference point
                _startTimestamp = nextFrame.BestEffortTimestamp;
                _isFirstFrame = false;
                frameToDisplay = _frameQueue.Dequeue();
            }
            else
            {
                // Calculate when this frame should be displayed
                var frameTimestamp = nextFrame.BestEffortTimestamp;
                var framePts = (frameTimestamp - _startTimestamp) * _timeBase;
                var elapsedSeconds = _playbackClock.Elapsed.TotalSeconds;
                
                // Display frame if its presentation time has arrived (with 2ms tolerance)
                if (elapsedSeconds >= framePts - 0.002)
                {
                    frameToDisplay = _frameQueue.Dequeue();
                    
                    // If we're behind, skip frames to catch up
                    while (_frameQueue.Count > 0)
                    {
                        var peekedFrame = _frameQueue.Peek();
                        var peekedPts = (peekedFrame.BestEffortTimestamp - _startTimestamp) * _timeBase;
                        
                        if (elapsedSeconds >= peekedPts + 0.002) // More than 2ms late
                        {
                            // Skip this frame
                            frameToDisplay?.Dispose();
                            frameToDisplay = _frameQueue.Dequeue();
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }
        }

        if (frameToDisplay != null)
        {
            Frame.SetFrame(frameToDisplay);
            _currentFrame?.Dispose();
            _currentFrame = frameToDisplay;
        }
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        _timer.Stop();
        _demuxer.Dispose();
        _decoder.Dispose();
        _currentFrame.Dispose();
        _packet.Dispose();
        
        lock (_frameQueue)
        {
            while (_frameQueue.Count > 0)
            {
                _frameQueue.Dequeue().Dispose();
            }
        }
        
        base.OnClosing(e);
    }
}