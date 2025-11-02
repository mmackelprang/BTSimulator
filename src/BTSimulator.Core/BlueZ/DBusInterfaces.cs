using System.Collections.Generic;
using System.Threading.Tasks;
using Tmds.DBus;

namespace BTSimulator.Core.BlueZ;

/// <summary>
/// D-Bus ObjectManager interface for enumerating objects.
/// </summary>
[DBusInterface("org.freedesktop.DBus.ObjectManager")]
public interface IObjectManager : IDBusObject
{
    Task<IDictionary<ObjectPath, IDictionary<string, IDictionary<string, object>>>> GetManagedObjectsAsync();
    Task<IDisposable> WatchInterfacesAddedAsync(Action<(ObjectPath objectPath, IDictionary<string, IDictionary<string, object>> interfaces)> handler);
    Task<IDisposable> WatchInterfacesRemovedAsync(Action<(ObjectPath objectPath, string[] interfaces)> handler);
}

/// <summary>
/// BlueZ Adapter1 interface.
/// </summary>
[DBusInterface("org.bluez.Adapter1")]
public interface IAdapter1 : IDBusObject
{
    Task StartDiscoveryAsync();
    Task StopDiscoveryAsync();
    Task RemoveDeviceAsync(ObjectPath device);
    Task SetDiscoveryFilterAsync(IDictionary<string, object> properties);
    
    Task<T> GetAsync<T>(string prop);
    Task SetAsync(string prop, object val);
    Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler);
}

/// <summary>
/// Properties for Adapter1 interface.
/// </summary>
public class Adapter1Properties
{
    public string Address { get; set; } = string.Empty;
    public string AddressType { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Alias { get; set; } = string.Empty;
    public uint Class { get; set; }
    public bool Powered { get; set; }
    public bool Discoverable { get; set; }
    public bool Pairable { get; set; }
    public uint PairableTimeout { get; set; }
    public uint DiscoverableTimeout { get; set; }
    public bool Discovering { get; set; }
    public string[] UUIDs { get; set; } = Array.Empty<string>();
    public string Modalias { get; set; } = string.Empty;

    /// <summary>
    /// Creates an Adapter1Properties instance from a D-Bus property dictionary.
    /// </summary>
    public static Adapter1Properties FromDictionary(IDictionary<string, object> properties)
    {
        var props = new Adapter1Properties();
        
        if (properties.TryGetValue("Address", out var address))
            props.Address = address as string ?? string.Empty;
        if (properties.TryGetValue("AddressType", out var addressType))
            props.AddressType = addressType as string ?? string.Empty;
        if (properties.TryGetValue("Name", out var name))
            props.Name = name as string ?? string.Empty;
        if (properties.TryGetValue("Alias", out var alias))
            props.Alias = alias as string ?? string.Empty;
        if (properties.TryGetValue("Class", out var classVal))
            props.Class = Convert.ToUInt32(classVal);
        if (properties.TryGetValue("Powered", out var powered))
            props.Powered = Convert.ToBoolean(powered);
        if (properties.TryGetValue("Discoverable", out var discoverable))
            props.Discoverable = Convert.ToBoolean(discoverable);
        if (properties.TryGetValue("Pairable", out var pairable))
            props.Pairable = Convert.ToBoolean(pairable);
        if (properties.TryGetValue("PairableTimeout", out var pairableTimeout))
            props.PairableTimeout = Convert.ToUInt32(pairableTimeout);
        if (properties.TryGetValue("DiscoverableTimeout", out var discoverableTimeout))
            props.DiscoverableTimeout = Convert.ToUInt32(discoverableTimeout);
        if (properties.TryGetValue("Discovering", out var discovering))
            props.Discovering = Convert.ToBoolean(discovering);
        if (properties.TryGetValue("UUIDs", out var uuids))
            props.UUIDs = uuids as string[] ?? Array.Empty<string>();
        if (properties.TryGetValue("Modalias", out var modalias))
            props.Modalias = modalias as string ?? string.Empty;
            
        return props;
    }
}

/// <summary>
/// LEAdvertisingManager1 interface for BLE advertising.
/// </summary>
[DBusInterface("org.bluez.LEAdvertisingManager1")]
public interface ILEAdvertisingManager1 : IDBusObject
{
    Task RegisterAdvertisementAsync(ObjectPath advertisement, IDictionary<string, object> options);
    Task UnregisterAdvertisementAsync(ObjectPath advertisement);
}

/// <summary>
/// GattManager1 interface for GATT service registration.
/// </summary>
[DBusInterface("org.bluez.GattManager1")]
public interface IGattManager1 : IDBusObject
{
    Task RegisterApplicationAsync(ObjectPath application, IDictionary<string, object> options);
    Task UnregisterApplicationAsync(ObjectPath application);
}

/// <summary>
/// BlueZ Device1 interface for device properties and connection.
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
    Task SetAsync(string prop, object val);
    Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler);
}

/// <summary>
/// Properties for Device1 interface.
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
