using System.Collections.Generic;
using System.Threading.Tasks;
using Tmds.DBus;
using BTSimulator.Core.BlueZ;

namespace BTSimulator.Scanner;

/// <summary>
/// BlueZ GattService1 interface
/// </summary>
[DBusInterface("org.bluez.GattService1")]
public interface IGattService1 : IDBusObject
{
}

/// <summary>
/// Properties for GattService1 interface
/// </summary>
public class GattService1Properties
{
    public string UUID { get; set; } = string.Empty;
    public bool Primary { get; set; }
    public ObjectPath Device { get; set; }
    public ObjectPath[]? Includes { get; set; }

    /// <summary>
    /// Creates a GattService1Properties instance from a D-Bus property dictionary.
    /// </summary>
    public static GattService1Properties FromDictionary(IDictionary<string, object> properties)
    {
        var props = new GattService1Properties();
        
        if (properties.TryGetValue("UUID", out var uuid))
            props.UUID = uuid as string ?? string.Empty;
        if (properties.TryGetValue("Primary", out var primary))
            props.Primary = Convert.ToBoolean(primary);
        if (properties.TryGetValue("Device", out var device))
            props.Device = (ObjectPath)device;
        if (properties.TryGetValue("Includes", out var includes))
            props.Includes = includes as ObjectPath[];
            
        return props;
    }
}

/// <summary>
/// BlueZ GattCharacteristic1 interface
/// </summary>
[DBusInterface("org.bluez.GattCharacteristic1")]
public interface IGattCharacteristic1 : IDBusObject
{
    Task<byte[]> ReadValueAsync(IDictionary<string, object> options);
    Task WriteValueAsync(byte[] value, IDictionary<string, object> options);
    Task<(ushort handle, ushort mtu)> AcquireWriteAsync(IDictionary<string, object> options);
    Task<(ushort handle, ushort mtu)> AcquireNotifyAsync(IDictionary<string, object> options);
    Task StartNotifyAsync();
    Task StopNotifyAsync();
}

/// <summary>
/// Properties for GattCharacteristic1 interface
/// </summary>
public class GattCharacteristic1Properties
{
    public string UUID { get; set; } = string.Empty;
    public ObjectPath Service { get; set; }
    public byte[]? Value { get; set; }
    public bool Notifying { get; set; }
    public string[]? Flags { get; set; }
    public ushort Handle { get; set; }

    /// <summary>
    /// Creates a GattCharacteristic1Properties instance from a D-Bus property dictionary.
    /// </summary>
    public static GattCharacteristic1Properties FromDictionary(IDictionary<string, object> properties)
    {
        var props = new GattCharacteristic1Properties();
        
        if (properties.TryGetValue("UUID", out var uuid))
            props.UUID = uuid as string ?? string.Empty;
        if (properties.TryGetValue("Service", out var service))
            props.Service = (ObjectPath)service;
        if (properties.TryGetValue("Value", out var value))
            props.Value = value as byte[];
        if (properties.TryGetValue("Notifying", out var notifying))
            props.Notifying = Convert.ToBoolean(notifying);
        if (properties.TryGetValue("Flags", out var flags))
            props.Flags = flags as string[];
        if (properties.TryGetValue("Handle", out var handle))
            props.Handle = Convert.ToUInt16(handle);
            
        return props;
    }
}
