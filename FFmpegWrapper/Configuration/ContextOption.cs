namespace FFmpegWrapper.Configuration;

using System.Collections.Generic;
using Core;
using Extensions;

/// <summary> Represents an option accepted by a ffmpeg object. </summary>
public readonly struct ContextOption(FFHandle<AVOption> handle)
{
    public FFHandle<AVOption> Handle {
        get {
            unsafe {
                return _handle;
            }
        }
    }
    
    public ReadOnlySpan<byte> Name {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            unsafe
            {
                return FFHelper.Utf8SpanFromPtrNullTerm(Handle.Ref.name);
            }
        }
    }
    
    public ReadOnlySpan<byte> Description {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            unsafe
            {
                return FFHelper.Utf8SpanFromPtrNullTerm(Handle.Ref.help);
            }
        }
    }


    public AVOptionType Type {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Handle.Ref.type;
    }

    public double MinValue {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Handle.Ref.min;
    } 
    public double MaxValue {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Handle.Ref.max;
    }

    /// <summary> Offset to the field containing this option, relative to the object pointer. </summary>
    public int Offset {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Handle.Ref.offset;
    }

    public OptionValue? DefaultValue {
        get {
            unsafe {
                var handle = Handle.Raw;
                return new OptionValue(&handle->u, handle->type);
            }
        }
    }

    private readonly unsafe AVOption* _handle = handle;


    /// <summary> Returns a list of acceptable pre-defined input values. </summary>
    public IReadOnlyList<ContextOption> GetNamedValues()
    {
        unsafe
        {
            if (_handle->unit == null) {
                return Array.Empty<ContextOption>();
            }
            var list = new List<ContextOption>();

            //The AVOption documentation says that AVClass options must be declared in
            //a static null terminated array, so this should be mostly fine.
            for (AVOption* opt = _handle + 1; opt->name != null; opt++) {
                if (opt->type == AVOptionType.AV_OPT_TYPE_CONST && opt->unit == _handle->unit) {
                    list.Add(new ContextOption(opt));
                }
            }
            return list;
        }
    }
    /// <summary> Sets the field of <paramref name="obj"/> with the given name to <paramref name="value"/>. </summary>
    /// <remarks>
    /// In case <paramref name="value"/> is a string and the field is not
    /// of a string type, the given string will be parsed.
    /// SI postfixes and some named scalars are supported. <br/>
    /// If the field is of a numeric type, it has to be a numeric or named scalar.
    /// Behavior with more than one scalar and +- infix operators is undefined. <br/>
    /// If the field is of a flags type, it has to be a sequence of numeric
    /// scalars or named flags separated by '+' or '-'. Prefixing a flag with '+' 
    /// causes it to be set without affecting the other flags; similarly, '-' unsets a flag. <br/>
    /// If the field is of a dictionary type, it has to be a ':' separated list of key = value parameters.
    /// Values containing ':' special characters must be escaped. <br/>
    /// </remarks>
    /// <param name="obj"> A struct whose first element is a pointer to an AVClass. </param>
    /// <param name="name"> The name of the field to set. </param>
    /// <param name="value"></param>
    public static unsafe void Set(void* obj, ReadOnlySpan<byte> name, OptionValue value, AVOptionSearchFlags flags = AVOptionSearchFlags.AV_OPT_SEARCH_CHILDREN)
    {
        var namePtr = name.RawHandle;
        
        //TODO
        int ret = value.ValueType switch {
            AVOptionType.AV_OPT_TYPE_STRING => av_opt_set(obj, namePtr, value._handle->str, (int)flags),
            AVOptionType.AV_OPT_TYPE_INT => av_opt_set_int(obj, namePtr, value._handle->i64, (int)flags),
            AVOptionType.AV_OPT_TYPE_DOUBLE => av_opt_set_double(obj, namePtr, value._handle->dbl, (int)flags),
            AVOptionType.AV_OPT_TYPE_RATIONAL => av_opt_set_q(obj, namePtr, value._handle->q, (int)flags),
        };
        
        if (ret < 0) {
            string className = FFHelper.PtrToStringUtf8((*(AVClass**)obj)->class_name);
            string strName = FFHelper.PtrToStringUtf8(namePtr);
            ret.ThrowError($"Invalid option for {className} (trying to set {strName} to {value.ValueType})");
        }
    }

    /// <summary> Gets the value of an option in <paramref name="obj"/> as a string. </summary>
    public static unsafe string GetAsString(void* obj, ReadOnlySpan<byte> name, AVOptionSearchFlags flags = 0)
    {
        byte* value;
        av_opt_get(obj, name.RawHandle, (int)flags, &value);

        var str = FFHelper.PtrToStringUtf8(value);
        av_free(value);
        return str;
    }

    /// <summary> Returns a list of options accepted by the specified ffmpeg object. </summary>
    /// <param name="removeAliases">Remove options that are short aliases to another.</param>
    /// <param name="skipDefaults"> Skip options whose value in <paramref name="obj"/> are set to default. </param>
    public static unsafe IReadOnlyList<ContextOption> GetOptions(void* obj, bool removeAliases = true, bool skipDefaults = false)
    {
        var opts = new List<ContextOption>();
        AVOption* iter = null;
        while ((iter = av_opt_next(obj, iter)) != null) {
            
            if (iter->type is AVOptionType.AV_OPT_TYPE_CONST || (skipDefaults && av_opt_is_set_to_default(obj, iter) is not 0)) 
                continue;

            opts.Add(new ContextOption(iter));
        }

        if (!removeAliases) {
            return opts;
        }

        opts.Sort((a, b) => a.Offset - b.Offset);

        int nextIgnoredOffset = -1;
        int j = 0;

        for (int i = 0; i < opts.Count; i++) {
            var opt = opts[i];

            while (i + 1 < opts.Count && opts[i + 1].Offset == opt.Offset) {
                var aliasOpt = opts[i + 1];

                if (aliasOpt.Type == AVOptionType.AV_OPT_TYPE_IMAGE_SIZE || aliasOpt.Name.Length > opt.Name.Length) {
                    opt = aliasOpt;
                }
                i++;
            }

            if (opt.Offset == nextIgnoredOffset) continue;

            if (opt.Type == AVOptionType.AV_OPT_TYPE_IMAGE_SIZE) {
                nextIgnoredOffset = opt.Offset + 4; //Ignore next height option
            }
            opts[j++] = opt;
        }
        
        opts.RemoveRange(j, opts.Count - j);
        return opts;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override string ToString()
    {
        return $"{FFHelper.SpanToStringUtf8(Name)}: {Type.ToString().ToLower().Substring("AV_OPT_TYPE_".Length)}";
    }
}
