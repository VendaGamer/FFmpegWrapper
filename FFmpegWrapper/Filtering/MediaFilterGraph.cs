namespace FFmpegWrapper.Filtering;

using System.Text;
using Abstractions;
using Configuration;

using Extensions;

using Hardware;
using Media;

public unsafe class MediaFilterGraph : FFObject<AVFilterGraph>
{
    public bool IsConfigured { get; private set; }

    public MediaFilterGraph()
    {
        _handle = avfilter_graph_alloc();
        
        if (_handle == null) {
            throw new OutOfMemoryException();
        }
    }

    public MediaFilterNode AddNode(MediaFilterArgs args)
    {
        ThrowIfConfigured();
        ThrowIfDisposed();
        //TODO
        return null;
    }

    public MediaBufferSource AddAudioBufferSource(AudioFormat format, Rational timeBase)
    {
        var pars = av_buffersrc_parameters_alloc();
        pars->format = (int)format.SampleFormat;
        pars->sample_rate = format.SampleRate;
        pars->ch_layout = format.Layout.Native;
        pars->time_base = timeBase;
        return AddBufferSource(pars, "abuffer"u8);
    }

    /// <param name="frameRate"> The frame rate of the input video. Must only be  set to a non-zero value if input stream has a known constant framerate and should be left at its initial value if the framerate is variable or unknown.</param>
    [Obsolete("Use AddVideoBufferSource(PictureFormat format, PictureColorspace colorspace, Rational timeBase, Rational? frameRate)")]
    public MediaBufferSource AddVideoBufferSource(PictureFormat format, Rational frameRate, Rational timeBase)
    {
        var pars = av_buffersrc_parameters_alloc();
        pars->width = format.Width;
        pars->height = format.Height;
        pars->format = (int)format.PixelFormat;
        pars->frame_rate = frameRate;
        pars->sample_aspect_ratio = format.PixelAspectRatio;
        pars->time_base = timeBase;
        return AddBufferSource(pars, "buffer"u8);
    }

    /// <param name="frameRate"> 
    /// The frame rate of the input video. 
    /// Must only be set to a non-zero value if input stream has a known constant framerate and should be left at its initial value (0/0) if the framerate is variable or unknown.
    /// See also <seealso cref="AVBufferSrcParameters.frame_rate"/> <seealso cref="AVFilterLink.frame_rate"/>
    /// </param>
    public MediaBufferSource AddVideoBufferSource(PictureFormat format, PictureColorspace colorspace, Rational timeBase, Rational? frameRate)
    {
        var pars = av_buffersrc_parameters_alloc();
        pars->width = format.Width;
        pars->height = format.Height;
        pars->format = (int)format.PixelFormat;
        pars->sample_aspect_ratio = format.PixelAspectRatio;
        pars->color_range = colorspace.Range;
        pars->color_space = colorspace.Matrix;
        pars->time_base = timeBase;
        if (frameRate.HasValue) {
            pars->frame_rate = frameRate.Value;
        }
        return AddBufferSource(pars, "buffer"u8);
    }

    private MediaBufferSource AddBufferSource(AVBufferSrcParameters* pars, ReadOnlySpan<byte> filterName)
    {
        ThrowIfConfigured();

        try {
            var node = avfilter_graph_alloc_filter(_handle,
                avfilter_get_by_name(filterName.RawHandle), "source"u8.RawHandle);
            
            av_buffersrc_parameters_set(node, pars).CheckError("Failed to set buffer source parameters");
            avfilter_init_str(node, null).CheckError("Failed to initialize buffer source node");
            return new MediaBufferSource(node);
        } finally {
            av_free(pars);
        }
    }

    public AudioBufferSink AddAudioBufferSink(MediaFilterNodePort input)
    {
        return new AudioBufferSink(AddBufferSink(input, "abuffersink"u8));
    }
    public VideoBufferSink AddVideoBufferSink(MediaFilterNodePort input)
    {
        return new VideoBufferSink(AddBufferSink(input, "buffersink"u8));
    }

    private AVFilterContext* AddBufferSink(MediaFilterNodePort input, ReadOnlySpan<byte> filterName)
    {
        ThrowIfConfigured();

        var node = avfilter_graph_alloc_filter(_handle,
            avfilter_get_by_name(filterName.RawHandle), "sink"u8.RawHandle);
        
        avfilter_init_str(node, null).CheckError("Failed to initialize buffer sink node");
        avfilter_link(input.Node.Handle, (uint)input.Index, node, 0).CheckError("Failed to link input node to buffer sink");
        return node;
    }

    /// <summary> Parses and initializes a graph segment described by the given string. </summary>
    /// <returns> A map of named outputs from the segment. </returns>
    public Dictionary<string, MediaFilterNodePort> Parse(ReadOnlySpan<byte> str)
    {
        ThrowIfDisposed();
        ThrowIfConfigured();

        AVFilterInOut* inputLinks = null;
        AVFilterInOut* outputLinks = null;

        try {
            //TODO

            //This function names inputs/outputs pars to the caller's perspective,
            //so output[i] is actually the input of some parsed node.
            avfilter_graph_parse_ptr(_handle, str.RawHandle, &outputLinks, &inputLinks, null).CheckError("Failed to parse filter graph");

            if (outputLinks != null) {
                throw new InvalidOperationException("Parsed filter graph cannot have open inputs");
            }
            var outputs = new Dictionary<string, MediaFilterNodePort>();

            for (AVFilterInOut* link = inputLinks; link != null; link = link->next) {
                string name = FFHelper.PtrToStringUtf8(link->name)!;
                var node = new MediaFilterNode(link->filter_ctx); //TODO: handle buffer sinks and other derived nodes
                outputs.Add(name, new MediaFilterNodePort(node, link->pad_idx));
            }
            return outputs;
        } finally {
            avfilter_inout_free(&inputLinks);
            avfilter_inout_free(&outputLinks);
        }
    }

    public void SetOption(ReadOnlySpan<byte> name, ReadOnlySpan<byte> value)
    {
        ThrowIfDisposed();
        ThrowIfConfigured();
        
        av_opt_set(_handle, name.RawHandle, value.RawHandle, 0).CheckError();
    }

    public void Configure()
    {
        ThrowIfDisposed();
        ThrowIfConfigured();

        avfilter_graph_config(_handle, null).CheckError();
        IsConfigured = true;
    }

    public override string ToString()
    {
        ThrowIfDisposed();

        var sb = new StringBuilder();
        for (int i = 0; i < _handle->nb_filters; i++) {
            if (i != 0) sb.Append(',');

            var node = _handle->filters[i];

            //Input ports
            for (int j = 0; j < node->nb_inputs; j++) {
                AVFilterLink* link = node->inputs[j];

                int srcNodeIdx = 0;
                int srcPortIdx = (int)(link->srcpad - link->src->output_pads);

                while (srcNodeIdx < _handle->nb_filters && _handle->filters[srcNodeIdx] != link->src) {
                    srcNodeIdx++;
                }
                PrintPort(srcNodeIdx, srcPortIdx);
            }
            //Filter name
            sb.Append(FFHelper.PtrToStringUtf8(node->filter->name));
            if (node->name != null) {
                sb.Append($"@{FFHelper.PtrToStringUtf8(node->name)}");
            }

            //Options
            var displayOpts = ContextOption.GetOptions(node->priv, removeAliases: true, skipDefaults: true);

            for (int j = 0; j < displayOpts.Count; j++) {
                var opt = displayOpts[j];

                sb.Append(j == 0 ? '=' : ':');
                sb.Append(opt.Name).Append('=');
                //sb.Append(ContextOption.GetAsString(node->priv, opt.Name));
            }

            //Outputs
            for (int j = 0; j < node->nb_outputs; j++) {
                PrintPort(i, j);
            }
        }
        return sb.ToString();

        void PrintPort(int nodeIdx, int portIdx)
        {
            sb.Append("[");
            do {
                sb.Append((char)('A' + (nodeIdx % 26)));
                nodeIdx /= 26;
            } while (nodeIdx != 0);

            sb.Append(portIdx + "]");
        }
    }

    protected override void Free()
    {
        fixed (AVFilterGraph** c = &_handle) {
            avfilter_graph_free(c);
        }
    }
    protected void ThrowIfConfigured()
    {
        if (IsConfigured) {
            throw new InvalidOperationException("Value must be set before the filter graph is configured.");
        }
    }
}

public class MediaFilterArgs
{
    public MediaFilter Filter { get; set; }
    public List<MediaFilterNodePort> Inputs { get; set; } = new();

    /// <summary> List of arguments used to initialize the filter. </summary>
    public List<(string Key, OptionValue Value)> Arguments { get; set; } = new();
    public string? NodeName { get; set; }

    public HardwareDevice? HardwareDevice { get; set; }

    public ReadOnlySpan<byte> FilterName {
        set => Filter = MediaFilter.Get(value);
    }
}

public class MediaFilterNode
{
    public FFHandle<AVFilterContext> Handle {
        get {
            unsafe {
                return _handle;
            }
        }
    }
    
    public string Name {
        get {
            unsafe
            {
                return FFHelper.PtrToStringUtf8(Handle.Raw->name);
            }
        }
    }

    public MediaFilter Filter {
        get {
            unsafe
            {
                return new MediaFilter(Handle.Raw->filter);
            }
        }
    }

    public MediaFilterNodePort GetOutput(int index)
    {
        unsafe
        {
            if (index < 0 || index >= Handle.Raw->nb_outputs) {
                throw new ArgumentOutOfRangeException();
            }
            return new MediaFilterNodePort(this, index);
        }
    }

    protected void ThrowIfDisposed()
    {
        if (Handle.IsNull) {
            throw new ObjectDisposedException(nameof(AVFilterContext));
        }
    }

    internal unsafe readonly AVFilterContext* _handle;
    
    internal MediaFilterNode(FFHandle<AVFilterContext> handle)
    {
        unsafe
        {
            _handle = handle;
        }
    }
}
public readonly struct MediaFilterNodePort(MediaFilterNode node, int index)
{
    public readonly MediaFilterNode Node = node;
    public readonly int Index = index;
    
    public AVMediaType Type {
        get {
            unsafe
            {
                return avfilter_pad_get_type(Node.Handle.Raw->output_pads, Index);
            }
        }
    }

    /// <summary> Whether this port has been connected to a filter input. </summary>
    public bool IsConnected {
        get {
            unsafe
            {
                return Node.Handle.Raw->outputs[Index] != null;
            }
        }
    }
}
