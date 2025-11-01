using Xunit;
using BTSimulator.Core.BlueZ;

namespace BTSimulator.Tests.BlueZ;

public class AdapterInfoTests
{
    [Fact]
    public void AdapterInfo_ToString_ReturnsFormattedString()
    {
        // Arrange
        var adapter = new AdapterInfo
        {
            Name = "hci0",
            Address = "AA:BB:CC:DD:EE:FF",
            Alias = "My Bluetooth Adapter",
            Path = "/org/bluez/hci0",
            Powered = true
        };

        // Act
        var result = adapter.ToString();

        // Assert
        Assert.Equal("hci0 (AA:BB:CC:DD:EE:FF) - My Bluetooth Adapter", result);
    }

    [Fact]
    public void AdapterInfo_PropertiesCanBeSet()
    {
        // Arrange & Act
        var adapter = new AdapterInfo
        {
            Name = "hci1",
            Address = "11:22:33:44:55:66",
            Alias = "Secondary Adapter",
            Path = "/org/bluez/hci1",
            Powered = false
        };

        // Assert
        Assert.Equal("hci1", adapter.Name);
        Assert.Equal("11:22:33:44:55:66", adapter.Address);
        Assert.Equal("Secondary Adapter", adapter.Alias);
        Assert.Equal("/org/bluez/hci1", adapter.Path);
        Assert.False(adapter.Powered);
    }
}
