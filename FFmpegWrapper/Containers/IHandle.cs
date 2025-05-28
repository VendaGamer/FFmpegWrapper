namespace FFmpeg.Wrapper;
/// <summary>
/// Defines a contract for objects that provide access to unmanaged FFmpeg handles.
/// This interface enables safe interoperability between managed wrapper objects and 
/// unmanaged FFmpeg.AutoGen structures by exposing the underlying native pointer.
/// </summary>
/// <typeparam name="T">
/// The FFmpeg.AutoGen unmanaged structure type that this handle represents.
/// Must be an unmanaged type (value type with no managed references).
/// </typeparam>
/// <remarks>
/// <para>
/// This interface is designed to work with FFmpeg.AutoGen library bindings, providing
/// a standardized way to access native FFmpeg structures from managed wrapper classes.
/// </para>
/// <para>
/// Classes implementing this interface should ensure proper lifetime management of the
/// underlying unmanaged resource to prevent memory leaks and access violations.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class AudioStream : IHandle&lt;AVStream&gt;
/// {
///     private unsafe AVStream* _stream;
///     
///     public unsafe AVStream* Handle => _stream;
///     
///     public AudioStream(unsafe AVStream* stream)
///     {
///         _stream = stream;
///     }
/// }
/// </code>
/// </example>
public interface IHandle<T> where T : unmanaged
{
    /// <summary>
    /// Gets a pointer to the underlying unmanaged FFmpeg structure.
    /// </summary>
    /// <value>
    /// A pointer to the native FFmpeg structure of type <typeparamref name="T"/>.
    /// This pointer provides direct access to the unmanaged memory where the
    /// FFmpeg structure is stored.
    /// </value>
    /// <returns>
    /// A pointer to the unmanaged FFmpeg structure, or null if the handle
    /// is invalid or has been disposed.
    /// </returns>
    unsafe T* Handle { get; }
}