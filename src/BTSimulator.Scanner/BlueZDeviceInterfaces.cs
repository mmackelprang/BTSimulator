using System.Collections.Generic;
using System.Threading.Tasks;
using Tmds.DBus;
using BTSimulator.Core.BlueZ;

namespace BTSimulator.Scanner;

/// <summary>
/// BlueZ Device1 interface for device properties and connection
/// </summary>
[DBusInterface("org.bluez.Device1")]
public interface IDevice1 : IDBusObject
{
    Task ConnectAsync();
    Task DisconnectAsync();
    Task ConnectProfileAsync(string uuid);
    Task DisconnectProfileAsync(string uuid);
    Task PairAsync();
    Task CancelPairingAsync();
}

/// <summary>
/// Properties for Device1 interface
/// </summary>
public class Device1Properties
{
    public string Address { get; set; } = string.Empty;
    public string AddressType { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Alias { get; set; }
    public uint Class { get; set; }
    public ushort Appearance { get; set; }
    public string? Icon { get; set; }
    public bool Paired { get; set; }
    public bool Trusted { get; set; }
    public bool Blocked { get; set; }
    public bool LegacyPairing { get; set; }
    public short RSSI { get; set; }
    public bool Connected { get; set; }
    public string[]? UUIDs { get; set; }
    public string? Modalias { get; set; }
    public ObjectPath Adapter { get; set; }
    public bool ServicesResolved { get; set; }

    /// <summary>
    /// Creates a Device1Properties instance from a D-Bus property dictionary.
    /// </summary>
    public static Device1Properties FromDictionary(IDictionary<string, object> properties)
    {
        var props = new Device1Properties();
        
        if (properties.TryGetValue("Address", out var address))
            props.Address = address as string ?? string.Empty;
        if (properties.TryGetValue("AddressType", out var addressType))
            props.AddressType = addressType as string ?? string.Empty;
        if (properties.TryGetValue("Name", out var name))
            props.Name = name as string ?? string.Empty;
        if (properties.TryGetValue("Alias", out var alias))
            props.Alias = alias as string;
        if (properties.TryGetValue("Class", out var classVal))
            props.Class = Convert.ToUInt32(classVal);
        if (properties.TryGetValue("Appearance", out var appearance))
            props.Appearance = Convert.ToUInt16(appearance);
        if (properties.TryGetValue("Icon", out var icon))
            props.Icon = icon as string;
        if (properties.TryGetValue("Paired", out var paired))
            props.Paired = Convert.ToBoolean(paired);
        if (properties.TryGetValue("Trusted", out var trusted))
            props.Trusted = Convert.ToBoolean(trusted);
        if (properties.TryGetValue("Blocked", out var blocked))
            props.Blocked = Convert.ToBoolean(blocked);
        if (properties.TryGetValue("LegacyPairing", out var legacyPairing))
            props.LegacyPairing = Convert.ToBoolean(legacyPairing);
        if (properties.TryGetValue("RSSI", out var rssi))
            props.RSSI = Convert.ToInt16(rssi);
        if (properties.TryGetValue("Connected", out var connected))
            props.Connected = Convert.ToBoolean(connected);
        if (properties.TryGetValue("UUIDs", out var uuids))
            props.UUIDs = uuids as string[];
        if (properties.TryGetValue("Modalias", out var modalias))
            props.Modalias = modalias as string;
        if (properties.TryGetValue("Adapter", out var adapter))
            props.Adapter = (ObjectPath)adapter;
        if (properties.TryGetValue("ServicesResolved", out var servicesResolved))
            props.ServicesResolved = Convert.ToBoolean(servicesResolved);
            
        return props;
    }
}

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
