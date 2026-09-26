namespace FFmpegWrapper.Extensions;

using System.Text;

public static class EncodingExtensions
{
#if NETSTANDARD2_0
    extension(Encoding encoding)
    {
        public void GetBytes(ReadOnlySpan<char> chars, Span<byte> bytes)
        {
            unsafe {
                fixed (char* cPtr = chars)
                fixed (byte* bPtr = bytes)
                    encoding.GetBytes(cPtr, chars.Length, bPtr, bytes.Length);
            }
        }
    }
#endif

}