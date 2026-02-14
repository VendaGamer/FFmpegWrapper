namespace FFmpegWrapper.Core;

public abstract class FFObjectBase<TRaw> : OwnedObject<TRaw>, IHandleOwner<TRaw>
    where TRaw : unmanaged
{
    public abstract Handle<TRaw> Handle { get; }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Handle<TRaw>(FFObjectBase<TRaw> ownedObject) => ownedObject.Handle;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe implicit operator TRaw*(FFObjectBase<TRaw> ownedObject) => ownedObject.Handle;
}