using System;
using System.Collections.Generic;
using Tmds.DBus;
using Xunit;
using BTSimulator.Core.BlueZ;
using BTSimulator.Scanner;

namespace BTSimulator.Tests.BlueZ;

/// <summary>
/// Tests for D-Bus property deserialization using FromDictionary methods.
/// </summary>
public class PropertyDeserializationTests
{
    [Fact]
    public void Adapter1Properties_FromDictionary_MapsAllProperties()
    {
        // Arrange
        var dict = new Dictionary<string, object>
        {
            { "Address", "AA:BB:CC:DD:EE:FF" },
            { "AddressType", "public" },
            { "Name", "TestAdapter" },
            { "Alias", "MyAdapter" },
            { "Class", (uint)1835268 },
            { "Powered", true },
            { "Discoverable", false },
            { "Pairable", true },
            { "PairableTimeout", (uint)180 },
            { "DiscoverableTimeout", (uint)0 },
            { "Discovering", false },
            { "UUIDs", new[] { "00001800-0000-1000-8000-00805f9b34fb" } },
            { "Modalias", "usb:v1D6Bp0246d0532" }
        };

        // Act
        var props = Adapter1Properties.FromDictionary(dict);

        // Assert
        Assert.Equal("AA:BB:CC:DD:EE:FF", props.Address);
        Assert.Equal("public", props.AddressType);
        Assert.Equal("TestAdapter", props.Name);
        Assert.Equal("MyAdapter", props.Alias);
        Assert.Equal((uint)1835268, props.Class);
        Assert.True(props.Powered);
        Assert.False(props.Discoverable);
        Assert.True(props.Pairable);
        Assert.Equal((uint)180, props.PairableTimeout);
        Assert.Equal((uint)0, props.DiscoverableTimeout);
        Assert.False(props.Discovering);
        Assert.Single(props.UUIDs);
        Assert.Equal("usb:v1D6Bp0246d0532", props.Modalias);
    }

    [Fact]
    public void Adapter1Properties_FromDictionary_HandlesEmptyDictionary()
    {
        // Arrange
        var dict = new Dictionary<string, object>();

        // Act
        var props = Adapter1Properties.FromDictionary(dict);

        // Assert
        Assert.NotNull(props);
        Assert.Equal(string.Empty, props.Address);
        Assert.Equal(string.Empty, props.Name);
        Assert.Empty(props.UUIDs);
    }

    [Fact]
    public void Device1Properties_FromDictionary_MapsAllProperties()
    {
        // Arrange
        var dict = new Dictionary<string, object>
        {
            { "Address", "11:22:33:44:55:66" },
            { "AddressType", "random" },
            { "Name", "TestDevice" },
            { "Alias", "MyDevice" },
            { "Class", (uint)2360324 },
            { "Appearance", (ushort)960 },
            { "Icon", "phone" },
            { "Paired", true },
            { "Trusted", false },
            { "Blocked", false },
            { "LegacyPairing", false },
            { "RSSI", (short)-65 },
            { "Connected", true },
            { "UUIDs", new[] { "0000180f-0000-1000-8000-00805f9b34fb" } },
            { "Modalias", "bluetooth:v004Cp0001d0001" },
            { "Adapter", new ObjectPath("/org/bluez/hci0") },
            { "ServicesResolved", true }
        };

        // Act
        var props = Device1Properties.FromDictionary(dict);

        // Assert
        Assert.Equal("11:22:33:44:55:66", props.Address);
        Assert.Equal("random", props.AddressType);
        Assert.Equal("TestDevice", props.Name);
        Assert.Equal("MyDevice", props.Alias);
        Assert.Equal((uint)2360324, props.Class);
        Assert.Equal((ushort)960, props.Appearance);
        Assert.Equal("phone", props.Icon);
        Assert.True(props.Paired);
        Assert.False(props.Trusted);
        Assert.False(props.Blocked);
        Assert.False(props.LegacyPairing);
        Assert.Equal((short)-65, props.RSSI);
        Assert.True(props.Connected);
        Assert.NotNull(props.UUIDs);
        Assert.Single(props.UUIDs);
        Assert.Equal("bluetooth:v004Cp0001d0001", props.Modalias);
        Assert.Equal("/org/bluez/hci0", props.Adapter.ToString());
        Assert.True(props.ServicesResolved);
    }

    [Fact]
    public void GattService1Properties_FromDictionary_MapsAllProperties()
    {
        // Arrange
        var dict = new Dictionary<string, object>
        {
            { "UUID", "0000180f-0000-1000-8000-00805f9b34fb" },
            { "Primary", true },
            { "Device", new ObjectPath("/org/bluez/hci0/dev_11_22_33_44_55_66") }
        };

        // Act
        var props = GattService1Properties.FromDictionary(dict);

        // Assert
        Assert.Equal("0000180f-0000-1000-8000-00805f9b34fb", props.UUID);
        Assert.True(props.Primary);
        Assert.Equal("/org/bluez/hci0/dev_11_22_33_44_55_66", props.Device.ToString());
    }

    [Fact]
    public void GattCharacteristic1Properties_FromDictionary_MapsAllProperties()
    {
        // Arrange
        var dict = new Dictionary<string, object>
        {
            { "UUID", "00002a19-0000-1000-8000-00805f9b34fb" },
            { "Service", new ObjectPath("/org/bluez/hci0/dev_11_22_33_44_55_66/service0010") },
            { "Value", new byte[] { 0x64 } },
            { "Notifying", true },
            { "Flags", new[] { "read", "notify" } },
            { "Handle", (ushort)20 }
        };

        // Act
        var props = GattCharacteristic1Properties.FromDictionary(dict);

        // Assert
        Assert.Equal("00002a19-0000-1000-8000-00805f9b34fb", props.UUID);
        Assert.Equal("/org/bluez/hci0/dev_11_22_33_44_55_66/service0010", props.Service.ToString());
        Assert.NotNull(props.Value);
        Assert.Single(props.Value);
        Assert.Equal((byte)0x64, props.Value[0]);
        Assert.True(props.Notifying);
        Assert.NotNull(props.Flags);
        Assert.Equal(2, props.Flags.Length);
        Assert.Equal((ushort)20, props.Handle);
    }
}
