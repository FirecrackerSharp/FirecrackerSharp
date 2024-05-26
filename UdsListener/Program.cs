using System.Text.Json.Serialization;

var builder = WebApplication.CreateSlimBuilder(args);
const string socketPath = "/tmp/uds-listener.sock";

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenUnixSocket(socketPath);
});

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
});

var app = builder.Build();

app.MapGet("/get/ok", () => new DataRecord(1));
app.MapGet("/get/bad-request", () => Results.BadRequest());
app.MapGet("/get/error", () =>
{
    throw new Exception();
});

app.MapPut("/put/ok", (DataRecord record) => record);
app.MapPut("/put/bad-request", (DataRecord _) => Results.BadRequest());
app.MapPut("/put/error", (DataRecord _) =>
{
    throw new Exception();
});

app.MapPatch("/patch/ok", (DataRecord record) => record);
app.MapPatch("/patch/bad-request", (DataRecord _) => Results.BadRequest());
app.MapPatch("/patch/error", () =>
{
    throw new Exception();
});

app.Run();

record DataRecord(int Field);

[JsonSerializable(typeof(DataRecord))]
internal partial class AppJsonSerializerContext : JsonSerializerContext;
