using FirecrackerSharp.Management;
using FluentAssertions;

namespace FirecrackerSharp.Tests;

public class ResponseTests
{
    [Fact]
    public void ThrowIfSuccess_ShouldThrow()
    {
        FluentActions.Invoking(() => Response.Success.ThrowIfSuccess())
            .Should().Throw<CheckFailedException>();
        FluentActions.Invoking(() => Response.BadRequest("br").ThrowIfSuccess())
            .Should().NotThrow<CheckFailedException>();

        FluentActions.Invoking(() => ResponseWith<Container>.Success(new Container()).ThrowIfSuccess())
            .Should().Throw<CheckFailedException>();
        FluentActions.Invoking(() => ResponseWith<Container>.BadRequest("br").ThrowIfSuccess())
            .Should().NotThrow<CheckFailedException>();
    }

    [Fact]
    public void ThrowIfError_ShouldThrow()
    {
        FluentActions.Invoking(() => Response.Success.ThrowIfError())
            .Should().NotThrow<CheckFailedException>();
        FluentActions.Invoking(() => Response.BadRequest("br").ThrowIfError())
            .Should().Throw<CheckFailedException>();

        FluentActions.Invoking(() => ResponseWith<Container>.Success(new Container()).ThrowIfError())
            .Should().NotThrow<CheckFailedException>();
        FluentActions.Invoking(() => ResponseWith<Container>.BadRequest("br").ThrowIfError())
            .Should().Throw<CheckFailedException>();
    }

    [Fact]
    public void IfSuccess_ShouldInvoke()
    {
        var t1 = false;
        var t2 = false;

        Response.Success.IfSuccess(() => t1 = true);
        t1.Should().BeTrue();
        Response.BadRequest("br").IfSuccess(() => t2 = true);
        t2.Should().BeFalse();

        var t3 = false;
        var t4 = false;
        var container = new Container();
        
        ResponseWith<Container>.Success(container).IfSuccess(content =>
        {
            content.Should().Be(container);
            t3 = true;
        });
        t3.Should().BeTrue();
        ResponseWith<Container>.BadRequest("br").IfSuccess(_ => t4 = true);
        t4.Should().BeFalse();
    }

    [Fact]
    public async Task IfSuccessAsync_ShouldInvoke()
    {
        var t1 = false;
        var t2 = false;

        await Response.Success.IfSuccessAsync(async () =>
        {
            await Task.Delay(10);
            t1 = true;
        });
        t1.Should().BeTrue();
        await Response.BadRequest("br").IfSuccessAsync(async () =>
        {
            await Task.Delay(10);
            t2 = true;
        });
        t2.Should().BeFalse();

        var t3 = false;
        var t4 = false;
        var container = new Container();
        
        await ResponseWith<Container>.Success(container).IfSuccessAsync(async value =>
        {
            value.Should().Be(container);
            await Task.Delay(10);
            t3 = true;
        });
        t3.Should().BeTrue();
        await ResponseWith<Container>.BadRequest("br").IfSuccessAsync(async value =>
        {
            await Task.Delay(10);
            t4 = true;
        });
        t4.Should().BeFalse();
    }

    [Fact]
    public void IfError_ShouldInvoke()
    {
        var t1 = false;
        var t2 = false;

        Response.Success.IfError(_ => t1 = true);
        t1.Should().BeFalse();
        Response.BadRequest("br").IfError(error =>
        {
            error.Should().Be("br");
            t2 = true;
        });
        t2.Should().BeTrue();

        var t3 = false;
        var t4 = false;

        ResponseWith<Container>.Success(new Container()).IfError(_ => t3 = true);
        t3.Should().BeFalse();
        ResponseWith<Container>.BadRequest("br").IfError(error =>
        {
            error.Should().Be("br");
            t4 = true;
        });
        t4.Should().BeTrue();
    }

    [Fact]
    public async Task IfErrorAsync_ShouldInvoke()
    {
        var t1 = false;
        var t2 = false;

        await Response.Success.IfErrorAsync(async _ =>
        {
            await Task.Delay(10);
            t1 = true;
        });
        t1.Should().BeFalse();
        await Response.BadRequest("br").IfErrorAsync(async error =>
        {
            error.Should().Be("br");
            await Task.Delay(10);
            t2 = true;
        });
        t2.Should().BeTrue();

        var t3 = false;
        var t4 = false;

        await ResponseWith<Container>.Success(new Container()).IfErrorAsync(async _ =>
        {
            await Task.Delay(10);
            t3 = true;
        });
        t3.Should().BeFalse();
        await ResponseWith<Container>.BadRequest("br").IfErrorAsync(async error =>
        {
            error.Should().Be("br");
            await Task.Delay(10);
            t4 = true;
        });
        t4.Should().BeTrue();
    }

    [Fact]
    public void Success_ShouldReportProperties()
    {
        var success1 = Response.Success;
        success1.Type.Should().Be(ResponseType.Success);
        success1.Error.Should().BeNull();
        success1.IsSuccess.Should().BeTrue();
        success1.IsFailure.Should().BeFalse();

        var container = new Container();
        var success2 = ResponseWith<Container>.Success(container);
        success2.Type.Should().Be(ResponseType.Success);
        success2.Error.Should().BeNull();
        success2.Content.Should().Be(container);
        success2.IsSuccess.Should().BeTrue();
        success2.IsFailure.Should().BeFalse();
    }

    [Fact]
    public void Error_ShouldReportProperties()
    {
        var error1 = Response.BadRequest("br");
        error1.Type.Should().Be(ResponseType.BadRequest);
        error1.Error.Should().Be("br");
        error1.IsSuccess.Should().BeFalse();
        error1.IsFailure.Should().BeTrue();

        var error2 = ResponseWith<Container>.BadRequest("br");
        error2.Type.Should().Be(ResponseType.BadRequest);
        error2.Error.Should().Be("br");
        error2.Content.Should().BeNull();
        error2.IsSuccess.Should().BeFalse();
        error2.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void ChainWith_ShouldProduceSimpleChains()
    {
        Response.Success.ChainWith(Response.Success).Should().Be(Response.Success);
        // S + BR = BR
        var r1 = Response.Success.ChainWith(Response.BadRequest("br"));
        r1.Type.Should().Be(ResponseType.BadRequest);
        r1.Error.Should().Be("br");
        // BR + S = BR
        var r2 = Response.BadRequest("br").ChainWith(Response.Success);
        r2.Type.Should().Be(ResponseType.BadRequest);
        r2.Error.Should().Be("br");
        // S + IR = IR
        var r3 = Response.Success.ChainWith(Response.InternalError("ir"));
        r3.Type.Should().Be(ResponseType.InternalError);
        r3.Error.Should().Be("ir");
        // IR + S = IR
        var r4 = Response.InternalError("ir").ChainWith(Response.Success);
        r4.Type.Should().Be(ResponseType.InternalError);
        r4.Error.Should().Be("ir");
    }

    [Fact]
    public void ChainWith_ShouldProduceComplexChains()
    {
        // IR + IR = merged IR
        var r1 = Response.InternalError("a").ChainWith(Response.InternalError("b"), ";");
        r1.Type.Should().Be(ResponseType.InternalError);
        r1.Error.Should().Be("b;a");
        // BR + BR = merged BR
        var r2 = Response.BadRequest("a").ChainWith(Response.BadRequest("b"), ";");
        r2.Type.Should().Be(ResponseType.BadRequest);
        r2.Error.Should().Be("b;a");
        // BR + IR = merged IR
        var r3 = Response.BadRequest("a").ChainWith(Response.InternalError("b"), ";");
        r3.Type.Should().Be(ResponseType.InternalError);
        r3.Error.Should().Be("b;a");
        // IR + BR = merged BR
        var r4 = Response.InternalError("a").ChainWith(Response.BadRequest("b"), ";");
        r4.Type.Should().Be(ResponseType.BadRequest);
        r4.Error.Should().Be("b;a");
    }

    // ReSharper disable once NotAccessedPositionalProperty.Local
    private record Container(int Value = 1);
}