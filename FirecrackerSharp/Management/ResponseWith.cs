namespace FirecrackerSharp.Management;

/// <summary>
/// A response from the Management API potentially containing content that was deserialized from a JSON response body.
/// </summary>
/// <typeparam name="TContent">The type of the potential content</typeparam>
public sealed class ResponseWith<TContent> where TContent : class
{
    /// <summary>
    /// The <see cref="ResponseType"/> of this response.
    /// </summary>
    public ResponseType Type { get; }
    /// <summary>
    /// The error if <see cref="Type"/> is a failure, or null if <see cref="Type"/> is a success
    /// </summary>
    public string? Error { get; }
    /// <summary>
    /// The <see cref="TContent"/> typed content or null if this is a failure response
    /// </summary>
    public TContent? Content { get; }
    
    /// <summary>
    /// Whether this response is a success according to its <see cref="Type"/>
    /// </summary>
    public bool IsSuccess => Type == ResponseType.Success;
    /// <summary>
    /// Whether this response is a failure according to its <see cref="Type"/>
    /// </summary>
    public bool IsFailure => Type != ResponseType.Success;

    private ResponseWith(ResponseType type, string? error, TContent? content)
    {
        Type = type;
        Error = error;
        Content = content;
    }
    
    /// <summary>
    /// Throw a <see cref="CheckFailedException"/> if this response is a success.
    /// </summary>
    /// <exception cref="CheckFailedException">The thrown exception</exception>
    public string ThrowIfSuccess()
    {
        if (Type == ResponseType.Success)
        {
            throw new CheckFailedException("Expected error, got success");
        }
        return Error!;
    }

    /// <summary>
    /// Throw a <see cref="CheckFailedException"/> if this response is an error.
    /// </summary>
    /// <exception cref="CheckFailedException">The thrown exception</exception>
    public TContent ThrowIfError()
    {
        if (Type != ResponseType.Success)
        {
            throw new CheckFailedException("Expected success, got error");
        }
        return Content!;
    }

    /// <summary>
    /// Runs the given action if this response is a success.
    /// </summary>
    /// <param name="action">The action to invoke, receiving the content as its parameter</param>
    /// <returns>Itself, for chaining calls</returns>
    public ResponseWith<TContent> IfSuccess(Action<TContent> action)
    {
        if (Type == ResponseType.Success) action(Content!);
        return this;
    }

    /// <summary>
    /// Awaits the given action asynchronously if this response is a success.
    /// </summary>
    /// <param name="asyncAction">The action to invoke asynchronously, receiving the content as its parameter</param>
    public async Task IfSuccessAsync(Func<TContent, Task> asyncAction)
    {
        if (Type == ResponseType.Success) await asyncAction(Content!);
    }

    /// <summary>
    /// Runs the given action if this response is an error.
    /// </summary>
    /// <param name="action">The action to invoke, receiving the error as its argument</param>
    /// <returns>Itself, for chaining calls</returns>
    public ResponseWith<TContent> IfError(Action<string> action)
    {
        if (Type != ResponseType.Success) action(Error!);
        return this;
    }

    /// <summary>
    /// Awaits the given action asynchronously if this response is an error.
    /// </summary>
    /// <param name="asyncAction">The action to invoke asynchronously, receiving the error as its argument</param>
    public async Task IfErrorAsync(Func<string, Task> asyncAction)
    {
        if (Type != ResponseType.Success) await asyncAction(Error!);
    }

    // Factory methods, intended only for usage by VM host implementations
    public static ResponseWith<TContent> Success(TContent content) =>
        new(ResponseType.Success, error: null, content);

    public static ResponseWith<TContent> BadRequest(string error) =>
        new(ResponseType.BadRequest, error, content: null);

    public static ResponseWith<TContent> InternalError(string error) =>
        new(ResponseType.InternalError, error, content: null);
}