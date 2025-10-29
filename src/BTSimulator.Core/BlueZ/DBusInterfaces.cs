using System.Collections.Generic;
using System.Threading.Tasks;
using Tmds.DBus;

namespace BTSimulator.Core.BlueZ;

/// <summary>
/// D-Bus Properties interface for getting and setting object properties.
/// </summary>
[DBusInterface("org.freedesktop.DBus.Properties")]
public interface IProperties : IDBusObject
{
    Task<object> GetAsync(string @interface, string property);
    Task<IDictionary<string, object>> GetAllAsync(string @interface);
    Task SetAsync(string @interface, string property, object value);
    Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler);
}

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
    Task<Adapter1Properties> GetAllAsync();
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
