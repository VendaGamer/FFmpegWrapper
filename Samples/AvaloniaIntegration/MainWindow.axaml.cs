using System.Numerics;
using Avalonia.Controls;
using AvaloniaIntegration.Extensions;
using SoundFlow.Components;
using SoundFlow.Interfaces;

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
using SoundFlow.Providers;
using SoundFlow.Structs;

public partial class MainWindow : Window
{
    public unsafe MainWindow()
    {
        InitializeComponent();
        FFmpegLinked.Init();
    }
}