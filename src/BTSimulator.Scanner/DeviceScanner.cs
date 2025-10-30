using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using BTSimulator.Core.BlueZ;
using BTSimulator.Core.Logging;
using Tmds.DBus;

namespace BTSimulator.Scanner;

/// <summary>
/// Scans for Bluetooth devices and extracts their properties
/// </summary>
public class DeviceScanner
{
    private readonly BlueZManager _manager;
    private readonly BlueZAdapter _adapter;
    private readonly ILogger _logger;
    private readonly Connection _connection;
    private static readonly HashSet<string> SecureReadFlags = new(new[]
    {
        "encrypt-read",
        "encrypt-authenticated-read",
        "secure-read",
        "authorize"
    }, StringComparer.OrdinalIgnoreCase);

    public DeviceScanner(BlueZManager manager, BlueZAdapter adapter, ILogger logger)
    {
        _manager = manager;
        _adapter = adapter;
        _logger = logger;
        _connection = Connection.System;
    }

    /// <summary>
    /// Scans for Bluetooth devices for the specified duration
    /// </summary>
    public async Task<List<ScannedDevice>> ScanForDevicesAsync(int durationSeconds)
    {
        var devices = new Dictionary<string, ScannedDevice>();

        try
        {
            await _adapter.StartDiscoveryAsync();
            await Task.Delay(durationSeconds * 1000);
            await _adapter.StopDiscoveryAsync();

            var objectManager = _connection.CreateProxy<IObjectManager>(BlueZConstants.Service, "/");
            var objects = await objectManager.GetManagedObjectsAsync();

            foreach (var entry in objects)
            {
                var objectPath = entry.Key;
                var objectPathString = objectPath.ToString();

                if (!objectPathString.StartsWith(_adapter.AdapterPath + "/"))
                    continue;

                if (!entry.Value.TryGetValue("org.bluez.Device1", out var deviceProperties))
                    continue;

                try
                {
                    var device = await ExtractDeviceInfoAsync(objectPath, deviceProperties, objects, objectManager);
                    if (device != null)
                    {
                        devices[objectPathString] = device;
                    }
                }
                catch (Exception ex)
                {
                    _logger.Warning($"Failed to extract device info for {objectPathString}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Error("Error during device scanning", ex);
            throw;
        }

        return devices.Values.ToList();
    }

    private async Task<ScannedDevice?> ExtractDeviceInfoAsync(
        ObjectPath devicePath,
        IDictionary<string, object> deviceProperties,
        IDictionary<ObjectPath, IDictionary<string, IDictionary<string, object>>> snapshot,
        IObjectManager objectManager)
    {
        var devicePathString = devicePath.ToString();

        try
        {
            var properties = Device1Properties.FromDictionary(deviceProperties);

            var device = new ScannedDevice
            {
                Path = devicePathString,
                Address = properties.Address,
                Name = properties.Name,
                Rssi = properties.RSSI,
                ServiceUuids = properties.UUIDs?.ToList() ?? new List<string>()
            };

            if (properties.ServicesResolved)
            {
                device.Services = await ExtractGattServicesAsync(devicePathString, snapshot);
                return device;
            }

            if (!ShouldAttemptGattResolution(properties))
            {
                return device;
            }

            try
            {
                var deviceProxy = _connection.CreateProxy<IDevice1>(BlueZConstants.Service, devicePathString);
                var connectedToDevice = false;

                if (!properties.Connected)
                {
                    _logger.Debug($"Connecting to {device.Name ?? device.Address} to read services...");
                    await deviceProxy.ConnectAsync();
                    connectedToDevice = true;

                    for (int i = 0; i < 10; i++)
                    {
                        await Task.Delay(500);
                        var updatedObjects = await objectManager.GetManagedObjectsAsync();
                        if (TryGetDeviceProperties(updatedObjects, devicePath, out var updatedPropsDict))
                        {
                            var updatedProps = Device1Properties.FromDictionary(updatedPropsDict);
                            if (updatedProps.ServicesResolved)
                            {
                                device.Services = await ExtractGattServicesAsync(devicePathString, updatedObjects);
                                break;
                            }
                        }
                    }
                }

                if (connectedToDevice)
                {
                    try
                    {
                        await deviceProxy.DisconnectAsync();
                    }
                    catch (DBusException dbusEx) when (dbusEx.ErrorName == "org.bluez.Error.NotConnected")
                    {
                        // Device already disconnected; nothing to do.
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Debug($"Could not connect to device {device.Name ?? device.Address}: {ex.Message}");
            }

            return device;
        }
        catch (Exception ex)
        {
            _logger.Warning($"Failed to extract device info: {ex.Message}");
            return null;
        }
    }

    private async Task<List<ScannedService>> ExtractGattServicesAsync(
        string devicePath,
        IDictionary<ObjectPath, IDictionary<string, IDictionary<string, object>>> objectsSnapshot)
    {
        var services = new List<ScannedService>();

        foreach (var entry in objectsSnapshot)
        {
            var objectPathString = entry.Key.ToString();

            if (!objectPathString.StartsWith(devicePath + "/service"))
                continue;

            if (!entry.Value.TryGetValue("org.bluez.GattService1", out var serviceProperties))
                continue;

            try
            {
                var serviceProps = GattService1Properties.FromDictionary(serviceProperties);
                var service = new ScannedService
                {
                    Uuid = serviceProps.UUID,
                    IsPrimary = serviceProps.Primary
                };

                service.Characteristics = await ExtractGattCharacteristicsAsync(objectsSnapshot, objectPathString);

                services.Add(service);
            }
            catch (Exception ex)
            {
                _logger.Debug($"Failed to extract service {objectPathString}: {ex.Message}");
            }
        }

        return services;
    }

    private async Task<List<ScannedCharacteristic>> ExtractGattCharacteristicsAsync(
        IDictionary<ObjectPath, IDictionary<string, IDictionary<string, object>>> objectsSnapshot,
        string servicePath)
    {
        var characteristics = new List<ScannedCharacteristic>();

        foreach (var entry in objectsSnapshot)
        {
            var objectPathString = entry.Key.ToString();

            if (!objectPathString.StartsWith(servicePath + "/char"))
                continue;

            if (!entry.Value.TryGetValue("org.bluez.GattCharacteristic1", out var characteristicProperties))
                continue;

            try
            {
                var charProps = GattCharacteristic1Properties.FromDictionary(characteristicProperties);

                var characteristic = new ScannedCharacteristic
                {
                    Uuid = charProps.UUID,
                    Flags = charProps.Flags?.ToList() ?? new List<string>()
                };

                if (SupportsUnauthenticatedRead(characteristic.Flags))
                {
                    try
                    {
                        var charProxy = _connection.CreateProxy<IGattCharacteristic1>(BlueZConstants.Service, objectPathString);
                        characteristic.Value = await charProxy.ReadValueAsync(new Dictionary<string, object>());
                    }
                    catch
                    {
                        characteristic.Value = null;
                    }
                }

                characteristics.Add(characteristic);
            }
            catch (Exception ex)
            {
                _logger.Debug($"Failed to extract characteristic {objectPathString}: {ex.Message}");
            }
        }

        return characteristics;
    }

    private static bool TryGetDeviceProperties(
        IDictionary<ObjectPath, IDictionary<string, IDictionary<string, object>>> objects,
        ObjectPath devicePath,
        [NotNullWhen(true)] out IDictionary<string, object>? properties)
    {
        if (objects.TryGetValue(devicePath, out var interfaces) &&
            interfaces.TryGetValue("org.bluez.Device1", out properties))
        {
            return true;
        }

        properties = null;
        return false;
    }

    private static bool ShouldAttemptGattResolution(Device1Properties properties)
    {
        if (properties.ServicesResolved)
        {
            return true;
        }

        if (properties.Connected)
        {
            return true;
        }

        if (!properties.Paired && !properties.Trusted)
        {
            return false;
        }

        return true;
    }

    private static bool SupportsUnauthenticatedRead(IReadOnlyCollection<string> flags)
    {
        if (!flags.Contains("read"))
        {
            return false;
        }

        return !flags.Any(flag => SecureReadFlags.Contains(flag));
    }
}

/// <summary>
/// Represents a scanned Bluetooth device
/// </summary>
public class ScannedDevice
{
    public string Path { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string? Name { get; set; }
    public short Rssi { get; set; }
    public List<string> ServiceUuids { get; set; } = new();
    public List<ScannedService> Services { get; set; } = new();
}

/// <summary>
/// Represents a scanned GATT service
/// </summary>
public class ScannedService
{
    public string Uuid { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
    public List<ScannedCharacteristic> Characteristics { get; set; } = new();
}

/// <summary>
/// Represents a scanned GATT characteristic
/// </summary>
public class ScannedCharacteristic
{
    public string Uuid { get; set; } = string.Empty;
    public List<string> Flags { get; set; } = new();
    public byte[]? Value { get; set; }
}
