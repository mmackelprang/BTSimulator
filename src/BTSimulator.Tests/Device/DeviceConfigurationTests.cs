using System;
using System.Linq;
using Xunit;
using BTSimulator.Core.Device;

namespace BTSimulator.Tests.Device;

/// <summary>
/// Tests for device configuration functionality.
/// </summary>
public class DeviceConfigurationTests
{
    [Fact]
    public void DeviceConfiguration_DefaultValues_ShouldBeSet()
    {
        // Arrange & Act
        var config = new DeviceConfiguration();

        // Assert
        Assert.Equal("BT Simulator", config.DeviceName);
        Assert.Null(config.DeviceAddress);
        Assert.Empty(config.Services);
    }

    [Fact]
    public void DeviceName_WhenSetToEmpty_ShouldThrowException()
    {
        // Arrange
        var config = new DeviceConfiguration();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => config.DeviceName = "");
        Assert.Throws<ArgumentException>(() => config.DeviceName = "   ");
    }

    [Fact]
    public void DeviceAddress_WhenSetToValidFormat_ShouldAccept()
    {
        // Arrange
        var config = new DeviceConfiguration();

        // Act
        config.DeviceAddress = "AA:BB:CC:DD:EE:FF";

        // Assert
        Assert.Equal("AA:BB:CC:DD:EE:FF", config.DeviceAddress);
    }

    [Theory]
    [InlineData("AA:BB:CC:DD:EE:GG")] // Invalid hex
    [InlineData("AA:BB:CC:DD:EE")]    // Too short
    [InlineData("AA-BB-CC-DD-EE-FF")] // Wrong separator
    [InlineData("AABBCCDDEEFF")]      // No separator
    public void DeviceAddress_WhenSetToInvalidFormat_ShouldThrowException(string invalidAddress)
    {
        // Arrange
        var config = new DeviceConfiguration();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => config.DeviceAddress = invalidAddress);
    }

    [Fact]
    public void AddService_ShouldAddToServicesList()
    {
        // Arrange
        var config = new DeviceConfiguration();
        var service = new GattServiceConfiguration
        {
            Uuid = "180F",
            IsPrimary = true
        };

        // Act
        config.AddService(service);

        // Assert
        Assert.Single(config.Services);
        Assert.Equal("180F", config.Services[0].Uuid);
    }

    [Fact]
    public void AddService_WithDuplicateUuid_ShouldThrowException()
    {
        // Arrange
        var config = new DeviceConfiguration();
        var service1 = new GattServiceConfiguration { Uuid = "180F" };
        var service2 = new GattServiceConfiguration { Uuid = "180F" };
        config.AddService(service1);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => config.AddService(service2));
    }

    [Fact]
    public void RemoveService_WhenExists_ShouldReturnTrue()
    {
        // Arrange
        var config = new DeviceConfiguration();
        var service = new GattServiceConfiguration { Uuid = "180F" };
        config.AddService(service);

        // Act
        var result = config.RemoveService("180F");

        // Assert
        Assert.True(result);
        Assert.Empty(config.Services);
    }

    [Fact]
    public void RemoveService_WhenNotExists_ShouldReturnFalse()
    {
        // Arrange
        var config = new DeviceConfiguration();

        // Act
        var result = config.RemoveService("180F");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ClearServices_ShouldRemoveAllServices()
    {
        // Arrange
        var config = new DeviceConfiguration();
        config.AddService(new GattServiceConfiguration { Uuid = "180F" });
        config.AddService(new GattServiceConfiguration { Uuid = "1810" });

        // Act
        config.ClearServices();

        // Assert
        Assert.Empty(config.Services);
    }

    [Fact]
    public void Validate_WithValidConfiguration_ShouldReturnTrue()
    {
        // Arrange
        var config = new DeviceConfiguration
        {
            DeviceName = "Test Device",
            DeviceAddress = "AA:BB:CC:DD:EE:FF"
        };
        var service = new GattServiceConfiguration { Uuid = "180F" };
        var characteristic = new GattCharacteristicConfiguration
        {
            Uuid = "2A19",
            Flags = new() { "read", "notify" }
        };
        service.AddCharacteristic(characteristic);
        config.AddService(service);

        // Act
        var isValid = config.Validate(out var errors);

        // Assert
        Assert.True(isValid);
        Assert.Empty(errors);
    }
}

/// <summary>
/// Tests for GATT service configuration.
/// </summary>
public class GattServiceConfigurationTests
{
    [Fact]
    public void GattService_DefaultValues_ShouldBeSet()
    {
        // Arrange & Act
        var service = new GattServiceConfiguration();

        // Assert
        Assert.Empty(service.Uuid);
        Assert.True(service.IsPrimary);
        Assert.Empty(service.Characteristics);
    }

    [Theory]
    [InlineData("180F")]                                           // 16-bit UUID
    [InlineData("0000180F-0000-1000-8000-00805F9B34FB")]          // 128-bit UUID
    public void Validate_WithValidUuid_ShouldReturnTrue(string uuid)
    {
        // Arrange
        var service = new GattServiceConfiguration { Uuid = uuid };

        // Act
        var isValid = service.Validate(out var errors);

        // Assert
        Assert.True(isValid);
        Assert.Empty(errors);
    }

    [Theory]
    [InlineData("")]                    // Empty
    [InlineData("180")]                 // Too short
    [InlineData("GGGG")]                // Invalid hex
    [InlineData("180F-0000")]           // Invalid format
    public void Validate_WithInvalidUuid_ShouldReturnFalse(string uuid)
    {
        // Arrange
        var service = new GattServiceConfiguration { Uuid = uuid };

        // Act
        var isValid = service.Validate(out var errors);

        // Assert
        Assert.False(isValid);
        Assert.NotEmpty(errors);
    }

    [Fact]
    public void AddCharacteristic_ShouldAddToList()
    {
        // Arrange
        var service = new GattServiceConfiguration { Uuid = "180F" };
        var characteristic = new GattCharacteristicConfiguration
        {
            Uuid = "2A19",
            Flags = new() { "read" }
        };

        // Act
        service.AddCharacteristic(characteristic);

        // Assert
        Assert.Single(service.Characteristics);
        Assert.Equal("2A19", service.Characteristics[0].Uuid);
    }

    [Fact]
    public void AddCharacteristic_WithDuplicateUuid_ShouldThrowException()
    {
        // Arrange
        var service = new GattServiceConfiguration { Uuid = "180F" };
        var char1 = new GattCharacteristicConfiguration { Uuid = "2A19", Flags = new() { "read" } };
        var char2 = new GattCharacteristicConfiguration { Uuid = "2A19", Flags = new() { "write" } };
        service.AddCharacteristic(char1);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => service.AddCharacteristic(char2));
    }
}

/// <summary>
/// Tests for GATT characteristic configuration.
/// </summary>
public class GattCharacteristicConfigurationTests
{
    [Fact]
    public void GattCharacteristic_DefaultValues_ShouldBeSet()
    {
        // Arrange & Act
        var characteristic = new GattCharacteristicConfiguration();

        // Assert
        Assert.Empty(characteristic.Uuid);
        Assert.Empty(characteristic.Flags);
        Assert.Empty(characteristic.InitialValue);
        Assert.Empty(characteristic.Description);
    }

    [Theory]
    [InlineData("read")]
    [InlineData("write")]
    [InlineData("notify")]
    [InlineData("indicate")]
    public void Validate_WithValidFlag_ShouldReturnTrue(string flag)
    {
        // Arrange
        var characteristic = new GattCharacteristicConfiguration
        {
            Uuid = "2A19",
            Flags = new() { flag }
        };

        // Act
        var isValid = characteristic.Validate(out var errors);

        // Assert
        Assert.True(isValid);
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_WithNoFlags_ShouldReturnFalse()
    {
        // Arrange
        var characteristic = new GattCharacteristicConfiguration
        {
            Uuid = "2A19",
            Flags = new()
        };

        // Act
        var isValid = characteristic.Validate(out var errors);

        // Assert
        Assert.False(isValid);
        Assert.Contains("At least one flag must be specified", errors);
    }

    [Fact]
    public void Validate_WithInvalidFlag_ShouldReturnFalse()
    {
        // Arrange
        var characteristic = new GattCharacteristicConfiguration
        {
            Uuid = "2A19",
            Flags = new() { "invalid-flag" }
        };

        // Act
        var isValid = characteristic.Validate(out var errors);

        // Assert
        Assert.False(isValid);
        Assert.Contains(errors, e => e.Contains("Invalid flags"));
    }

    [Fact]
    public void InitialValue_CanBeSet()
    {
        // Arrange
        var characteristic = new GattCharacteristicConfiguration
        {
            Uuid = "2A19",
            Flags = new() { "read" },
            InitialValue = new byte[] { 0x42, 0xFF }
        };

        // Act & Assert
        Assert.Equal(2, characteristic.InitialValue.Length);
        Assert.Equal(0x42, characteristic.InitialValue[0]);
        Assert.Equal(0xFF, characteristic.InitialValue[1]);
    }
}
