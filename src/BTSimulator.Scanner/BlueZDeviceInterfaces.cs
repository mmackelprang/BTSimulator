using System.Collections.Generic;
using System.Threading.Tasks;
using Tmds.DBus;

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
    
    Task<T> GetAsync<T>(string prop);
    Task<Device1Properties> GetAllAsync();
    Task SetAsync(string prop, object val);
    Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler);
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
}

/// <summary>
/// BlueZ GattService1 interface
/// </summary>
[DBusInterface("org.bluez.GattService1")]
public interface IGattService1 : IDBusObject
{
    Task<T> GetAsync<T>(string prop);
    Task<GattService1Properties> GetAllAsync();
    Task SetAsync(string prop, object val);
    Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler);
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
    
    Task<T> GetAsync<T>(string prop);
    Task<GattCharacteristic1Properties> GetAllAsync();
    Task SetAsync(string prop, object val);
    Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler);
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
}
