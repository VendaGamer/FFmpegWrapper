namespace FFmpegWrapper.Extensions;

using Core;

public static class AVErrorExtensions
{
    extension(AVError result)
    {
        public bool IsSuccess {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (int)result >= 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ThrowIfError(string? msg = null)
        {
            if (result < 0)
                FFHelper.ThrowError((int)result, msg);
        }
    }
}
