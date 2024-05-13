namespace FirecrackerSharp.Management;

public class ManagementResponse
{
    private readonly object? _content;
    private readonly string? _faultMessage;
    private readonly ManagementResponseType _type;

    private ManagementResponse(object? content, string? faultMessage, ManagementResponseType type)
    {
        _content = content;
        _faultMessage = faultMessage;
        _type = type;
    }

    public T? TryUnwrap<T>() where T : class
    {
        if (_type != ManagementResponseType.Success) return null;
        return _content as T;
    }

    public T UnwrapOrThrow<T>() where T : class
    {
        var unwrapped = TryUnwrap<T>();
        if (unwrapped is null) throw new ArgumentNullException(nameof(unwrapped), "Could not unwrap response");
        return unwrapped;
    }

    public static readonly ManagementResponse NoContent = new(null, null, ManagementResponseType.Success);
    
    public static ManagementResponse Ok<T>(T content) => new(content, null, ManagementResponseType.Success);
    public static ManagementResponse BadRequest(string faultMessage) => new(null, faultMessage, ManagementResponseType.BadRequest);
    public static ManagementResponse InternalError(string faultMessage) => new(null, faultMessage, ManagementResponseType.InternalError);
}