namespace FFmpegWrapper.Processing;

public sealed class BitStreamFilter : FFObject<AVBSFContext>
{
    private bool _isInit = false;
    
    public BitStreamFilter(Handle<AVBitStreamFilter> filter)
    {
        unsafe
        {
            AVBSFContext* ctx = null;
            av_bsf_alloc(filter, &ctx);
            _handle = ctx;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe bool Init() => _isInit = av_bsf_init(Handle) is 0;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe void CopyFrom(Handle<AVCodecParameters> parameters) => avcodec_parameters_copy(Handle.Ref.par_out, parameters);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe AVError Read(Handle<AVPacket> packet) => (AVError)av_bsf_receive_packet(Handle, packet);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe AVError Write(Handle<AVPacket> packet) => (AVError)av_bsf_send_packet(Handle, packet);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe void Flush() => av_bsf_flush(Handle);

    protected override unsafe void Free()
    {
        AVBSFContext* ctx = _handle;
        av_bsf_free(&ctx);
        _handle = ctx;
    }
}