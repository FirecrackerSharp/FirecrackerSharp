var builder = WebApplication.CreateSlimBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenUnixSocket("/opt/fs/.sock");
});

var app = builder.Build();

app.MapGet("/info", () => "info");

app.Run();
