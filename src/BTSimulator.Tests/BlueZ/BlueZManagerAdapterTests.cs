using Xunit;
using BTSimulator.Core.BlueZ;
using System.Reflection;

namespace BTSimulator.Tests.BlueZ;

public class BlueZManagerAdapterTests
{
    [Theory]
    [InlineData("/org/bluez/hci0", "hci0")]
    [InlineData("/org/bluez/hci1", "hci1")]
    [InlineData("/org/bluez/hci10", "hci10")]
    [InlineData("hci0", "hci0")]
    [InlineData("/a/b/c/adapter", "adapter")]
    public void ExtractAdapterName_ReturnsCorrectName(string path, string expectedName)
    {
        // Arrange - Use reflection to access private static method
        var method = typeof(BlueZManager).GetMethod(
            "ExtractAdapterName",
            BindingFlags.NonPublic | BindingFlags.Static
        );
        
        Assert.NotNull(method);

        // Act
        var result = method.Invoke(null, new object[] { path }) as string;

        // Assert
        Assert.Equal(expectedName, result);
    }
}
