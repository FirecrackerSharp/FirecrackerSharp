namespace FirecrackerSharp.Management;

public sealed class Response
{
    public ResponseType Type { get; }
    public string? Error { get; }
    
    public bool IsSuccess => Type == ResponseType.Success;
    public bool IsFailure => Type != ResponseType.Success;

    private Response(ResponseType type, string? error)
    {
        Type = type;
        Error = error;
    }

    public string ThrowIfSuccess()
    {
        if (Type == ResponseType.Success)
        {
            throw new CheckFailedException("Expected error, got success");
        }
        return Error!;
    }

    public void ThrowIfError()
    {
        if (Type != ResponseType.Success)
        {
            throw new CheckFailedException("Expected success, got error");
        }
    }

    public Response IfSuccess(Action action)
    {
        if (Type == ResponseType.Success) action();
        return this;
    }

    public async Task<Response> IfSuccessAsync(Func<Task> asyncAction)
    {
        if (Type == ResponseType.Success) await asyncAction();
        return this;
    }
    
    public Response IfError(Action<string> action)
    {
        if (Type != ResponseType.Success) action(Error!);
        return this;
    }

    public async Task<Response> IfErrorAsync(Func<string, Task> asyncAction)
    {
        if (Type != ResponseType.Success) await asyncAction(Error!);
        return this;
    }

    public Response ChainWith(Response otherResponse, string errorSplit = "; chained with: \n")
    {
        if (otherResponse == Success && this == Success) return Success;
        
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

    public static Response BadRequest(string error) => new(ResponseType.BadRequest, error);

    public static Response InternalError(string error) => new(ResponseType.InternalError, error);
}