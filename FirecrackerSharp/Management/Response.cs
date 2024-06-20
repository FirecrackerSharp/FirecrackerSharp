namespace FirecrackerSharp.Management;

/// <summary>
/// A response from the Management API that contains either a failure or a success without any content (e.g. empty
/// underlying request body). This maps to the 204, 400 and 500 HTTP response codes.
/// </summary>
public sealed class Response
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
    /// Whether this response is a success according to its <see cref="Type"/>
    /// </summary>
    public bool IsSuccess => Type == ResponseType.Success;
    /// <summary>
    /// Whether this response is a failure according to its <see cref="Type"/>
    /// </summary>
    public bool IsFailure => Type != ResponseType.Success;

    private Response(ResponseType type, string? error)
    {
        Type = type;
        Error = error;
    }

    /// <summary>
    /// Throw a <see cref="CheckFailedException"/> if this response is a success.
    /// </summary>
    /// <exception cref="CheckFailedException">The thrown exception</exception>
    public void ThrowIfSuccess()
    {
        if (Type == ResponseType.Success)
        {
            throw new CheckFailedException("Expected error, got success");
        }
    }

    /// <summary>
    /// Throw a <see cref="CheckFailedException"/> if this response is an error.
    /// </summary>
    /// <exception cref="CheckFailedException">The thrown exception</exception>
    public void ThrowIfError()
    {
        if (Type != ResponseType.Success)
        {
            throw new CheckFailedException("Expected success, got error");
        }
    }

    /// <summary>
    /// Runs the given action if this response is a success.
    /// </summary>
    /// <param name="action">The action to invoke</param>
    /// <returns>Itself, for chaining calls</returns>
    public Response IfSuccess(Action action)
    {
        if (Type == ResponseType.Success) action();
        return this;
    }

    /// <summary>
    /// Awaits the given action asynchronously if this response is a success.
    /// </summary>
    /// <param name="asyncAction">The action to invoke asynchronously</param>
    public async Task IfSuccessAsync(Func<Task> asyncAction)
    {
        if (Type == ResponseType.Success) await asyncAction();
    }
    
    /// <summary>
    /// Runs the given action if this response is an error.
    /// </summary>
    /// <param name="action">The action to invoke, receiving the error as its argument</param>
    /// <returns>Itself, for chaining calls</returns>
    public Response IfError(Action<string> action)
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

    /// <summary>
    /// Combine/chain this response with another one. If both are successes, you'll get a success. If one is an error,
    /// you'll get that error according to the original error type. If both are errors, you'll get an error response
    /// with the other response's error type and a combined error message.
    /// </summary>
    /// <param name="otherResponse">The other response to chain to this one</param>
    /// <param name="errorSplit">The separator between two errors when joining them</param>
    /// <returns>The chained response, allowing multiple calls</returns>
    public Response ChainWith(Response otherResponse, string errorSplit = "; chained with: \n")
    {
        if (otherResponse == Success && this == Success) return Success;
        if (otherResponse == Success) return this;
        
        if (otherResponse.Type == ResponseType.BadRequest)
        {
            return this == Success
                ? otherResponse
                : BadRequest(otherResponse.Error! + errorSplit + Error!);
        }

        return this == Success
            ? otherResponse
            : InternalError(otherResponse.Error! + errorSplit + Error!);
    }
    
    public static readonly Response Success = new(ResponseType.Success, error: null);

    // Factory methods, intended only for usage by VM host implementations
    public static Response BadRequest(string error) => new(ResponseType.BadRequest, error);

    public static Response InternalError(string error) => new(ResponseType.InternalError, error);
}