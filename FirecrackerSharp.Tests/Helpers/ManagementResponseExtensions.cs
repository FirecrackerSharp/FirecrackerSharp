using FirecrackerSharp.Management;
using FluentAssertions;

namespace FirecrackerSharp.Tests.Helpers;

public static class ManagementResponseExtensions
{
    public static void ShouldSucceed(this ManagementResponse managementResponse)
    {
        managementResponse.IsSuccessful.Should().BeTrue();
        
        (managementResponse == ManagementResponse.NoContent).Should()
            .BeTrue("the management response contains content despite no content being expected");
    }

    public static T ShouldSucceedWith<T>(this ManagementResponse managementResponse) where T : class
    {
        var containedObject = managementResponse.TryUnwrap<T>();
        
        managementResponse.IsSuccessful.Should().BeTrue();
        containedObject.Should().NotBeNull("the management response doesn't contain expected content");

        return containedObject!;
;    }
}