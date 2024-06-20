namespace FirecrackerSharp.Management;

public sealed class ResponseWith<TContent> where TContent : class
{
    public ResponseType Type { get; }
    public string? Error { get; }
    public TContent? Content { get; }
    
    public bool IsSuccess => Type == ResponseType.Success;
    public bool IsFailure => Type != ResponseType.Success;

    private ResponseWith(ResponseType type, string? error, TContent? content)
    {
        Type = type;
        Error = error;
        Content = content;
    }
    
    public string ThrowIfSuccess()
    {
        if (Type == ResponseType.Success)
        {
            throw new CheckFailedException("Expected error, got success");
        }
        return Error!;
    }

    public TContent ThrowIfError()
    {
        if (Type != ResponseType.Success)
        {
            throw new CheckFailedException("Expected success, got error");
        }
        return Content!;
    }

    public ResponseWith<TContent> IfSuccess(Action<TContent> action)
    {
        if (Type == ResponseType.Success) action(Content!);
        return this;
    }

    public async Task<ResponseWith<TContent>> IfSuccessAsync(Func<TContent, Task> asyncAction)
    {
        if (Type == ResponseType.Success) await asyncAction(Content!);
        return this;
    }

    public ResponseWith<TContent> IfError(Action<string> action)
    {
        if (Type != ResponseType.Success) action(Error!);
        return this;
    }

    public async Task<ResponseWith<TContent>> IfErrorAsync(Func<string, Task> asyncAction)
    {
        if (Type != ResponseType.Success) await asyncAction(Error!);
        return this;
    }

    public static ResponseWith<TContent> Success(TContent content) =>
        new(ResponseType.Success, error: null, content);

    public static ResponseWith<TContent> BadRequest(string error) =>
        new(ResponseType.BadRequest, error, content: null);

    public static ResponseWith<TContent> InternalError(string error) =>
        new(ResponseType.InternalError, error, content: null);
}