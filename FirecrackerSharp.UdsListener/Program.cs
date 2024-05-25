var builder = WebApplication.CreateBuilder(args);
const string socketPath = "/tmp/uds-listener.sock";

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenUnixSocket(socketPath);
});

var app = builder.Build();

app.MapGet("/get/ok", () => Results.Ok("content"));
app.MapGet("/get/bad-request", () => Results.BadRequest("bad-request"));
app.MapGet("/get/error", () =>
{
    throw new Exception();
});

app.MapPut("/put/ok", () => Results.Ok("content"));
app.MapPut("/put/bad-request", () => Results.BadRequest("bad-request"));
app.MapPut("/put/error", () =>
{
    throw new Exception();
});

app.MapPatch("/patch/ok", () => Results.Ok("content"));
app.MapPatch("/patch/bad-request", () => Results.BadRequest("bad-request"));
app.MapPatch("/patch/error", () =>
{
    throw new Exception();
});

app.Run();