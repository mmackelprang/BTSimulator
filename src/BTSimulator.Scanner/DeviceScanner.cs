using System.Collections.Generic;
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
            // Start discovery
            await _adapter.StartDiscoveryAsync();

            // Scan for the specified duration
            await Task.Delay(durationSeconds * 1000);

            // Stop discovery
            await _adapter.StopDiscoveryAsync();

            // Get all discovered devices
            var objectManager = _connection.CreateProxy<IObjectManager>(BlueZConstants.Service, "/");
            var objects = await objectManager.GetManagedObjectsAsync();

            foreach (var obj in objects)
            {
                var objPath = obj.Key.ToString();
                
                // Check if this is a device object under our adapter
                if (!objPath.StartsWith(_adapter.AdapterPath + "/"))
                    continue;

                // Check if object has Device1 interface
                if (!obj.Value.ContainsKey("org.bluez.Device1"))
                    continue;

                try
                {
                    var device = await ExtractDeviceInfoAsync(objPath);
                    if (device != null)
                    {
                        devices[objPath] = device;
                    }
                }
                catch (Exception ex)
                {
                    _logger.Warning($"Failed to extract device info for {objPath}: {ex.Message}");
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

    private async Task<ScannedDevice?> ExtractDeviceInfoAsync(string devicePath)
    {
        try
        {
            var deviceProxy = _connection.CreateProxy<IDevice1>(BlueZConstants.Service, devicePath);
            var properties = await deviceProxy.GetAllAsync();

            var device = new ScannedDevice
            {
                Path = devicePath,
                Address = properties.Address,
                Name = properties.Name,
                Rssi = properties.RSSI,
                ServiceUuids = properties.UUIDs?.ToList() ?? new List<string>()
            };

            // Try to connect to the device to read GATT services
            if (properties.ServicesResolved)
            {
                device.Services = await ExtractGattServicesAsync(devicePath);
            }
            else
            {
                // Try to connect and resolve services
                try
                {
                    if (!properties.Connected)
                    {
                        _logger.Debug($"Connecting to {device.Name ?? device.Address} to read services...");
                        await deviceProxy.ConnectAsync();
                        
                        // Wait for services to be resolved
                        for (int i = 0; i < 10; i++)
                        {
                            await Task.Delay(500);
                            var updatedProps = await deviceProxy.GetAllAsync();
                            if (updatedProps.ServicesResolved)
                            {
                                device.Services = await ExtractGattServicesAsync(devicePath);
                                break;
                            }
                        }

                        // Disconnect after reading
                        await deviceProxy.DisconnectAsync();
                    }
                }
                catch (Exception ex)
                {
                    _logger.Debug($"Could not connect to device {device.Name ?? device.Address}: {ex.Message}");
                }
            }

            return device;
        }
        catch (Exception ex)
        {
            _logger.Warning($"Failed to extract device info: {ex.Message}");
            return null;
        }
    }

    private async Task<List<ScannedService>> ExtractGattServicesAsync(string devicePath)
    {
        var services = new List<ScannedService>();

        try
        {
            var objectManager = _connection.CreateProxy<IObjectManager>(BlueZConstants.Service, "/");
            var objects = await objectManager.GetManagedObjectsAsync();

            foreach (var obj in objects)
            {
                var objPath = obj.Key.ToString();

                // Check if this is a service under the device
                if (!objPath.StartsWith(devicePath + "/service"))
                    continue;

                if (!obj.Value.ContainsKey("org.bluez.GattService1"))
                    continue;

                try
                {
                    var serviceProxy = _connection.CreateProxy<IGattService1>(BlueZConstants.Service, objPath);
                    var serviceProps = await serviceProxy.GetAllAsync();

                    var service = new ScannedService
                    {
                        Uuid = serviceProps.UUID,
                        IsPrimary = serviceProps.Primary
                    };

                    // Extract characteristics
                    service.Characteristics = await ExtractGattCharacteristicsAsync(objPath);

                    services.Add(service);
                }
                catch (Exception ex)
                {
                    _logger.Debug($"Failed to extract service {objPath}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Debug($"Failed to extract GATT services: {ex.Message}");
        }

        return services;
    }

    private async Task<List<ScannedCharacteristic>> ExtractGattCharacteristicsAsync(string servicePath)
    {
        var characteristics = new List<ScannedCharacteristic>();

        try
        {
            var objectManager = _connection.CreateProxy<IObjectManager>(BlueZConstants.Service, "/");
            var objects = await objectManager.GetManagedObjectsAsync();

            foreach (var obj in objects)
            {
                var objPath = obj.Key.ToString();

                // Check if this is a characteristic under the service
                if (!objPath.StartsWith(servicePath + "/char"))
                    continue;

                if (!obj.Value.ContainsKey("org.bluez.GattCharacteristic1"))
                    continue;

                try
                {
                    var charProxy = _connection.CreateProxy<IGattCharacteristic1>(BlueZConstants.Service, objPath);
                    var charProps = await charProxy.GetAllAsync();

                    var characteristic = new ScannedCharacteristic
                    {
                        Uuid = charProps.UUID,
                        Flags = charProps.Flags?.ToList() ?? new List<string>()
                    };

                    // Try to read the value if the characteristic is readable
                    if (characteristic.Flags.Contains("read"))
                    {
                        try
                        {
                            characteristic.Value = await charProxy.ReadValueAsync(new Dictionary<string, object>());
                        }
                        catch
                        {
                            // Some characteristics may not be readable without authentication
                            characteristic.Value = null;
                        }
                    }

                    characteristics.Add(characteristic);
                }
                catch (Exception ex)
                {
                    _logger.Debug($"Failed to extract characteristic {objPath}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Debug($"Failed to extract characteristics: {ex.Message}");
        }

        return characteristics;
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
