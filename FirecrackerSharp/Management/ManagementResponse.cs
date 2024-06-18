namespace FirecrackerSharp.Management;

/// <summary>
/// A potential response from the Firecracker Management API.
/// 
/// This response encapsulates the relevant data from an HTTP response received internally by the SDK and can be one of
/// the following: a success with no or some content (200/204), a bad request (400) and an internal server error (500).
/// </summary>
public sealed class ManagementResponse
{
    private readonly object? _content;
    private readonly string? _faultMessage;
    private readonly ManagementResponseType _type;

    /// <summary>
    /// Whether this response is a success.
    /// </summary>
    public bool IsSuccessful => _type == ManagementResponseType.Success;
    /// <summary>
    /// Whether this response is an error (either on the API user's or the API server's end).
    /// </summary>
    public bool IsError => _type != ManagementResponseType.Success;

    private ManagementResponse(object? content, string? faultMessage, ManagementResponseType type)
    {
        _content = content;
        _faultMessage = faultMessage;
        _type = type;
    }

    /// <summary>
    /// Try to safely unwrap this response's error type and fault message.
    /// </summary>
    /// <returns>A tuple containing both the response type (may not be an error) and the fault message (can be null if
    /// this isn't an error) of this response.</returns>
    public (ManagementResponseType, string?) TryUnwrapError()
    {
        return IsError ? (_type, _faultMessage!) : (_type, null);
    }

    /// <summary>
    /// Try to unwrap this response's error type and fault message and throw an exception upon failure.
    /// </summary>
    /// <returns>A tuple containing both</returns>
    /// <exception cref="ArgumentException"></exception>
    public (ManagementResponseType, string) UnwrapErrorOrThrow()
    {
        if (!IsError) throw new ArgumentException("Threw because tried to unwrap a response that wasn't an error");
        return (_type, _faultMessage!);
    }

    /// <summary>
    /// Try to safely unwrap this response's content as a certain type.
    /// </summary>
    /// <typeparam name="T">The expected type of the response's content</typeparam>
    /// <returns>Either the response content as <see cref="T"/> or null if there's no content or if the response doesn't indicate success</returns>
    public T? TryUnwrap<T>() where T : class
    {
        if (_type != ManagementResponseType.Success) return null;
        return _content as T;
    }

    /// <summary>
    /// Try to unwrap this response's content as a certain type and throw an exception upon failure.
    /// </summary>
    /// <typeparam name="T">The expected type of the response's content</typeparam>
    /// <returns>The non-nullable response content as <see cref="T"/></returns>
    /// <exception cref="ArgumentNullException">If there's no content or if the response doesn't indicate success</exception>
    public T UnwrapOrThrow<T>() where T : class
    {
        var unwrapped = TryUnwrap<T>();
        if (unwrapped is null) throw new ArgumentNullException(nameof(unwrapped), "Could not unwrap response");
        return unwrapped;
    }

    /// <summary>
    /// A successful <see cref="ManagementResponse"/> with no content inside.
    /// </summary>
    public static readonly ManagementResponse NoContent = new(null, null, ManagementResponseType.Success);
    
    /// <summary>
    /// Creates a successful <see cref="ManagementResponse"/> with given content inside.
    /// </summary>
    /// <param name="content">The encapsulated content of given type</param>
    /// <typeparam name="T">The type of the encapsulated content</typeparam>
    /// <returns>The created <see cref="ManagementResponse"/></returns>
    public static ManagementResponse Ok<T>(T content) => new(content, null, ManagementResponseType.Success);
    
    /// <summary>
    /// Creates an unsuccessful <see cref="ManagementResponse"/> due to an error on the API user's end.
    /// </summary>
    /// <param name="faultMessage">The message indicating what is wrong with the request sent by the API user.</param>
    /// <returns>The created <see cref="ManagementResponse"/></returns>
    public static ManagementResponse BadRequest(string faultMessage) => new(null, faultMessage, ManagementResponseType.BadRequest);
    
    /// <summary>
    /// Creates an unsuccessful <see cref="ManagementResponse"/> due to an error on the API server's end.
    /// </summary>
    /// <param name="faultMessage">The message indicating what went wrong on the API server.</param>
    /// <returns>The created <see cref="Mana"/></returns>
    public static ManagementResponse InternalError(string faultMessage) => new(null, faultMessage, ManagementResponseType.InternalError);
}