namespace FFmpegWrapper;

public readonly struct PixelFormatDescriptor : IHandleObserver<AVPixFmtDescriptor>
{

#region StaticProperties

    public static ImmutableArray<PixelFormatDescriptor> AvailableDescriptors => Utils.GetAllAvailableDescriptors();

    /// <summary>
    /// Workaround class.
    /// Cannot be directly in MediaCodec struct cause of this issue:
    /// https://github.com/dotnet/runtime/issues/104511
    /// </summary>
    private static class Utils
    {
        private static ImmutableArray<PixelFormatDescriptor> s_availableDescriptors;
            
        public static ImmutableArray<PixelFormatDescriptor> GetAllAvailableDescriptors()
        {
                
            if (!s_availableDescriptors.IsDefault) {
                return s_availableDescriptors;
            }
                
            var builder = ImmutableArray.CreateBuilder<PixelFormatDescriptor>();
                
            unsafe {
                AVPixFmtDescriptor* desc = null;
                
                while ((desc = av_pix_fmt_desc_next(desc)) is not null) {
                    builder.Add(new PixelFormatDescriptor(desc));
                }
            }
                
            s_availableDescriptors =  builder.ToImmutable();
            return s_availableDescriptors;
        }
    }

#endregion
    
    
#region Properties

    public unsafe Handle<AVPixFmtDescriptor> Handle {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _handle;
    }

    /// <inheritdoc cref="AVPixFmtDescriptor.name" />
    public unsafe ReadOnlySpan<byte> Name {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => FFHelper.Utf8SpanFromPtrNullTerm(Handle.Ref.name);
    }
    
    /// <inheritdoc cref="AVPixFmtDescriptor.alias" />
    public unsafe ReadOnlySpan<byte> Alias {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => FFHelper.Utf8SpanFromPtrNullTerm(Handle.Ref.alias);
    }

    /// <inheritdoc cref="AVPixFmtDescriptor.log2_chroma_w" />
    public byte LogChromaWidth {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            return Handle.Ref.log2_chroma_w;
        }
    }

    /// <inheritdoc cref="AVPixFmtDescriptor.log2_chroma_h" />
    public byte LogChromaHeight {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            return Handle.Ref.log2_chroma_h;
        }
    }

    /// <inheritdoc cref="AVPixFmtDescriptor.comp" />
    public unsafe ReadOnlySpan<AVComponentDescriptor> Log {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(&Handle.Raw->comp._0, 4);
    }
    
    /// <inheritdoc cref="AVPixFmtDescriptor.nb_components" />
    public byte ComponentsCount {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Handle.Ref.nb_components;
    }

    /// <inheritdoc cref="AVPixFmtDescriptor.flags" />
    public AVPixFmtFlags Flags {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (AVPixFmtFlags)Handle.Ref.flags;
    }

    /// <summary> Pixel format being described. </summary>

    public unsafe AVPixelFormat PixelFormat {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => av_pix_fmt_desc_get_id(Handle);
    }

    #endregion

    internal readonly unsafe AVPixFmtDescriptor* _handle;
    
#region Constructors
    
    public unsafe PixelFormatDescriptor(Handle<AVPixFmtDescriptor> handle) => _handle = handle;
    
    public unsafe PixelFormatDescriptor(AVPixelFormat format) => _handle = av_pix_fmt_desc_get(format);
    
#endregion
    
    
}