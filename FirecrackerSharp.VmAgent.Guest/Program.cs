var builder = WebApplication.CreateSlimBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenUnixSocket("/opt/ga/ga.sock");
});

var app = builder.Build();

app.MapGet("/info", () => "info");

app.Run();
