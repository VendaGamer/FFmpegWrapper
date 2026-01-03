namespace FFmpegWrapper.Extensions;

using Core;

public static class LavResultExtensions
{
    public static bool IsSuccess(this LavResult result)
    {
        return result >= LavResult.Success;
    }
    public static void ThrowIfError(this LavResult result, string? msg = null)
    {
        if (result < LavResult.Success) {
            ((int)result).ThrowError(msg);
        }
    }
}
