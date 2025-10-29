using System;
using System.Collections.Generic;
using Tmds.DBus;

namespace BTSimulator.Core.Gatt;

/// <summary>
/// Represents a BLE advertisement that can be registered with BlueZ.
/// </summary>
public class LEAdvertisement
{
    private readonly ObjectPath _objectPath;
    private readonly List<string> _serviceUUIDs = new();
    private readonly Dictionary<ushort, byte[]> _manufacturerData = new();
    private readonly Dictionary<string, object> _serviceData = new();

    public LEAdvertisement(string objectPath = "/com/btsimulator/advertisement")
    {
        _objectPath = new ObjectPath(objectPath);
    }

    /// <summary>
    /// Gets the D-Bus object path for this advertisement.
    /// </summary>
    public ObjectPath ObjectPath => _objectPath;

    /// <summary>
    /// Gets or sets the advertisement type.
    /// Valid values: "broadcast", "peripheral"
    /// </summary>
    public string Type { get; set; } = "peripheral";

    /// <summary>
    /// Gets the list of service UUIDs to advertise.
    /// </summary>
    public IReadOnlyList<string> ServiceUUIDs => _serviceUUIDs.AsReadOnly();

    /// <summary>
    /// Gets the manufacturer data dictionary.
    /// Key: Manufacturer ID (e.g., 0xFFFF for testing)
    /// Value: Manufacturer specific data
    /// </summary>
    public IReadOnlyDictionary<ushort, byte[]> ManufacturerData => _manufacturerData;

    /// <summary>
    /// Gets or sets the local name to advertise.
    /// </summary>
    public string? LocalName { get; set; }

    /// <summary>
    /// Gets or sets whether to include TX power in the advertisement.
    /// </summary>
    public bool IncludeTxPower { get; set; }

    /// <summary>
    /// Adds a service UUID to advertise.
    /// </summary>
    public void AddServiceUUID(string uuid)
    {
        if (string.IsNullOrWhiteSpace(uuid))
            throw new ArgumentException("UUID cannot be empty", nameof(uuid));

        if (!_serviceUUIDs.Contains(uuid))
        {
            _serviceUUIDs.Add(uuid);
        }
    }

    /// <summary>
    /// Adds manufacturer data to the advertisement.
    /// </summary>
    public void AddManufacturerData(ushort manufacturerId, byte[] data)
    {
        if (data == null)
            throw new ArgumentNullException(nameof(data));

        _manufacturerData[manufacturerId] = data;
    }

    /// <summary>
    /// Gets the D-Bus properties for this advertisement.
    /// </summary>
    public Dictionary<string, Dictionary<string, object>> GetProperties()
    {
        var props = new Dictionary<string, object>
        {
            ["Type"] = Type
        };

        if (_serviceUUIDs.Count > 0)
        {
            props["ServiceUUIDs"] = _serviceUUIDs.ToArray();
        }

        if (_manufacturerData.Count > 0)
        {
            props["ManufacturerData"] = _manufacturerData;
        }

        if (!string.IsNullOrEmpty(LocalName))
        {
            props["LocalName"] = LocalName;
        }

        if (IncludeTxPower)
        {
            props["IncludeTxPower"] = true;
        }

        return new Dictionary<string, Dictionary<string, object>>
        {
            ["org.bluez.LEAdvertisement1"] = props
        };
    }
}
