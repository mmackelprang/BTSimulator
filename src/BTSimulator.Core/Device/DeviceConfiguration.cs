using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace BTSimulator.Core.Device;

/// <summary>
/// Configuration for a simulated Bluetooth LE device.
/// Allows runtime configuration of device properties including name, address, and GATT services.
/// </summary>
public class DeviceConfiguration
{
    private string _deviceName = "BT Simulator";
    private string? _deviceAddress;
    private readonly List<GattServiceConfiguration> _services = new();

    /// <summary>
    /// Gets or sets the device name that will be advertised.
    /// This is the name that appears when scanning for BLE devices.
    /// </summary>
    public string DeviceName
    {
        get => _deviceName;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Device name cannot be empty", nameof(value));
            _deviceName = value;
        }
    }

    /// <summary>
    /// Gets or sets the Bluetooth MAC address for the device.
    /// Format: XX:XX:XX:XX:XX:XX (e.g., "AA:BB:CC:DD:EE:FF")
    /// Note: Changing MAC address requires special permissions and may not work on all systems.
    /// </summary>
    public string? DeviceAddress
    {
        get => _deviceAddress;
        set
        {
            if (value != null && !IsValidMacAddress(value))
                throw new ArgumentException("Invalid MAC address format. Expected format: XX:XX:XX:XX:XX:XX", nameof(value));
            _deviceAddress = value;
        }
    }

    /// <summary>
    /// Gets the list of GATT services to advertise.
    /// </summary>
    public IReadOnlyList<GattServiceConfiguration> Services => _services.AsReadOnly();

    /// <summary>
    /// Adds a GATT service to the device configuration.
    /// </summary>
    public void AddService(GattServiceConfiguration service)
    {
        if (service == null)
            throw new ArgumentNullException(nameof(service));
        
        if (_services.Any(s => s.Uuid.Equals(service.Uuid, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"Service with UUID {service.Uuid} already exists");
        
        _services.Add(service);
    }

    /// <summary>
    /// Removes a GATT service from the device configuration.
    /// </summary>
    public bool RemoveService(string uuid)
    {
        var service = _services.FirstOrDefault(s => s.Uuid.Equals(uuid, StringComparison.OrdinalIgnoreCase));
        return service != null && _services.Remove(service);
    }

    /// <summary>
    /// Clears all GATT services from the configuration.
    /// </summary>
    public void ClearServices()
    {
        _services.Clear();
    }

    /// <summary>
    /// Validates the device configuration.
    /// </summary>
    public bool Validate(out List<string> errors)
    {
        errors = new List<string>();

        if (string.IsNullOrWhiteSpace(DeviceName))
            errors.Add("Device name is required");

        if (_deviceAddress != null && !IsValidMacAddress(_deviceAddress))
            errors.Add("Device address format is invalid");

        foreach (var service in _services)
        {
            if (!service.Validate(out var serviceErrors))
                errors.AddRange(serviceErrors.Select(e => $"Service {service.Uuid}: {e}"));
        }

        return errors.Count == 0;
    }

    private static bool IsValidMacAddress(string address)
    {
        // Match XX:XX:XX:XX:XX:XX format where X is hexadecimal
        var regex = new Regex(@"^([0-9A-Fa-f]{2}:){5}[0-9A-Fa-f]{2}$");
        return regex.IsMatch(address);
    }
}

/// <summary>
/// Configuration for a GATT service.
/// A GATT service groups related characteristics together.
/// </summary>
public class GattServiceConfiguration
{
    private readonly List<GattCharacteristicConfiguration> _characteristics = new();

    /// <summary>
    /// Gets or sets the service UUID.
    /// Can be a 16-bit UUID (e.g., "180F" for Battery Service) 
    /// or 128-bit UUID (e.g., "0000180F-0000-1000-8000-00805F9B34FB")
    /// </summary>
    public string Uuid { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether this is a primary service.
    /// Primary services are top-level services, while secondary services are referenced by primary services.
    /// </summary>
    public bool IsPrimary { get; set; } = true;

    /// <summary>
    /// Gets the list of characteristics in this service.
    /// </summary>
    public IReadOnlyList<GattCharacteristicConfiguration> Characteristics => _characteristics.AsReadOnly();

    /// <summary>
    /// Adds a characteristic to this service.
    /// </summary>
    public void AddCharacteristic(GattCharacteristicConfiguration characteristic)
    {
        if (characteristic == null)
            throw new ArgumentNullException(nameof(characteristic));

        if (_characteristics.Any(c => c.Uuid.Equals(characteristic.Uuid, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"Characteristic with UUID {characteristic.Uuid} already exists");

        _characteristics.Add(characteristic);
    }

    /// <summary>
    /// Validates the service configuration.
    /// </summary>
    public bool Validate(out List<string> errors)
    {
        errors = new List<string>();

        if (string.IsNullOrWhiteSpace(Uuid))
            errors.Add("Service UUID is required");
        else if (!IsValidUuid(Uuid))
            errors.Add("Service UUID format is invalid");

        foreach (var characteristic in _characteristics)
        {
            if (!characteristic.Validate(out var charErrors))
                errors.AddRange(charErrors.Select(e => $"Characteristic {characteristic.Uuid}: {e}"));
        }

        return errors.Count == 0;
    }

    private static bool IsValidUuid(string uuid)
    {
        // Match 16-bit UUID (4 hex digits) or 128-bit UUID (standard format)
        var regex16 = new Regex(@"^[0-9A-Fa-f]{4}$");
        var regex128 = new Regex(@"^[0-9A-Fa-f]{8}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{12}$");
        
        return regex16.IsMatch(uuid) || regex128.IsMatch(uuid);
    }
}

/// <summary>
/// Configuration for a GATT characteristic.
/// A characteristic represents a single data value with specific permissions.
/// </summary>
public class GattCharacteristicConfiguration
{
    /// <summary>
    /// Gets or sets the characteristic UUID.
    /// </summary>
    public string Uuid { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the characteristic flags (permissions).
    /// Common flags: "read", "write", "notify", "indicate", "write-without-response"
    /// </summary>
    public List<string> Flags { get; set; } = new();

    /// <summary>
    /// Gets or sets the initial value of the characteristic.
    /// </summary>
    public byte[] InitialValue { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Gets or sets the description of the characteristic.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Validates the characteristic configuration.
    /// </summary>
    public bool Validate(out List<string> errors)
    {
        errors = new List<string>();

        if (string.IsNullOrWhiteSpace(Uuid))
            errors.Add("Characteristic UUID is required");
        else if (!IsValidUuid(Uuid))
            errors.Add("Characteristic UUID format is invalid");

        if (Flags.Count == 0)
            errors.Add("At least one flag must be specified");

        var validFlags = new[] { "read", "write", "write-without-response", "notify", "indicate", "broadcast", "authenticated-signed-writes" };
        var invalidFlags = Flags.Where(f => !validFlags.Contains(f.ToLower())).ToList();
        if (invalidFlags.Any())
            errors.Add($"Invalid flags: {string.Join(", ", invalidFlags)}");

        return errors.Count == 0;
    }

    private static bool IsValidUuid(string uuid)
    {
        var regex16 = new Regex(@"^[0-9A-Fa-f]{4}$");
        var regex128 = new Regex(@"^[0-9A-Fa-f]{8}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{12}$");
        
        return regex16.IsMatch(uuid) || regex128.IsMatch(uuid);
    }
}
