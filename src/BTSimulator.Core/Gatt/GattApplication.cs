using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tmds.DBus;
using BTSimulator.Core.Logging;

namespace BTSimulator.Core.Gatt;

/// <summary>
/// Represents a GATT application that can be registered with BlueZ.
/// A GATT application is a collection of GATT services.
/// </summary>
public class GattApplication : IDisposable
{
    private readonly List<GattService> _services = new();
    private readonly ObjectPath _objectPath;
    private bool _disposed;

    public GattApplication(string objectPath = "/com/btsimulator/app")
    {
        _objectPath = new ObjectPath(objectPath);
    }

    /// <summary>
    /// Gets the D-Bus object path for this application.
    /// </summary>
    public ObjectPath ObjectPath => _objectPath;

    /// <summary>
    /// Gets the list of services in this application.
    /// </summary>
    public IReadOnlyList<GattService> Services => _services.AsReadOnly();

    /// <summary>
    /// Adds a GATT service to this application.
    /// </summary>
    public void AddService(GattService service)
    {
        if (service == null)
            throw new ArgumentNullException(nameof(service));

        if (_services.Any(s => s.UUID == service.UUID))
            throw new InvalidOperationException($"Service with UUID {service.UUID} already exists");

        _services.Add(service);
    }

    /// <summary>
    /// Removes a GATT service from this application.
    /// </summary>
    public bool RemoveService(string uuid)
    {
        var service = _services.FirstOrDefault(s => s.UUID == uuid);
        return service != null && _services.Remove(service);
    }

    /// <summary>
    /// Gets all object paths managed by this application (services + characteristics + descriptors).
    /// </summary>
    public Dictionary<ObjectPath, Dictionary<string, Dictionary<string, object>>> GetManagedObjects()
    {
        var objects = new Dictionary<ObjectPath, Dictionary<string, Dictionary<string, object>>>();

        // Add each service and its characteristics
        foreach (var service in _services)
        {
            // Add service
            objects[service.ObjectPath] = service.GetProperties();

            // Add characteristics
            foreach (var characteristic in service.Characteristics)
            {
                objects[characteristic.ObjectPath] = characteristic.GetProperties();

                // Add descriptors
                foreach (var descriptor in characteristic.Descriptors)
                {
                    objects[descriptor.ObjectPath] = descriptor.GetProperties();
                }
            }
        }

        return objects;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            foreach (var service in _services)
            {
                service.Dispose();
            }
            _services.Clear();
            _disposed = true;
        }
    }
}

/// <summary>
/// Represents a GATT service.
/// </summary>
public class GattService : IDisposable
{
    private readonly List<GattCharacteristic> _characteristics = new();
    private readonly ObjectPath _objectPath;
    private bool _disposed;

    public GattService(string uuid, bool primary = true, int index = 0)
    {
        UUID = uuid ?? throw new ArgumentNullException(nameof(uuid));
        Primary = primary;
        _objectPath = new ObjectPath($"/com/btsimulator/service{index:D4}");
    }

    public string UUID { get; }
    public bool Primary { get; }
    public ObjectPath ObjectPath => _objectPath;

    /// <summary>
    /// Gets the list of characteristics in this service.
    /// </summary>
    public IReadOnlyList<GattCharacteristic> Characteristics => _characteristics.AsReadOnly();

    /// <summary>
    /// Adds a characteristic to this service.
    /// </summary>
    public void AddCharacteristic(GattCharacteristic characteristic)
    {
        if (characteristic == null)
            throw new ArgumentNullException(nameof(characteristic));

        if (_characteristics.Any(c => c.UUID == characteristic.UUID))
            throw new InvalidOperationException($"Characteristic with UUID {characteristic.UUID} already exists");

        _characteristics.Add(characteristic);
    }

    /// <summary>
    /// Gets the D-Bus properties for this service.
    /// </summary>
    public Dictionary<string, Dictionary<string, object>> GetProperties()
    {
        var props = new Dictionary<string, object>
        {
            ["UUID"] = UUID,
            ["Primary"] = Primary,
            ["Characteristics"] = _characteristics.Select(c => c.ObjectPath).ToArray()
        };

        return new Dictionary<string, Dictionary<string, object>>
        {
            ["org.bluez.GattService1"] = props
        };
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            foreach (var characteristic in _characteristics)
            {
                characteristic.Dispose();
            }
            _characteristics.Clear();
            _disposed = true;
        }
    }
}

/// <summary>
/// Represents a GATT characteristic.
/// </summary>
public class GattCharacteristic : IDisposable
{
    private readonly List<GattDescriptor> _descriptors = new();
    private readonly ObjectPath _objectPath;
    private byte[] _value;
    private bool _disposed;
    private ILogger _logger = NullLogger.Instance;

    public GattCharacteristic(string uuid, string[] flags, byte[]? initialValue = null, int index = 0, int serviceIndex = 0)
    {
        UUID = uuid ?? throw new ArgumentNullException(nameof(uuid));
        Flags = flags ?? throw new ArgumentNullException(nameof(flags));
        _value = initialValue ?? Array.Empty<byte>();
        _objectPath = new ObjectPath($"/com/btsimulator/service{serviceIndex:D4}/char{index:D4}");
    }

    /// <summary>
    /// Sets the logger for this characteristic.
    /// </summary>
    public void SetLogger(ILogger logger)
    {
        _logger = logger ?? NullLogger.Instance;
    }

    public string UUID { get; }
    public string[] Flags { get; }
    public ObjectPath ObjectPath => _objectPath;
    public ObjectPath ServicePath { get; set; } = new ObjectPath("/");

    /// <summary>
    /// Gets or sets the current value of this characteristic.
    /// </summary>
    public byte[] Value
    {
        get => _value;
        set => _value = value ?? Array.Empty<byte>();
    }

    /// <summary>
    /// Gets the list of descriptors for this characteristic.
    /// </summary>
    public IReadOnlyList<GattDescriptor> Descriptors => _descriptors.AsReadOnly();

    /// <summary>
    /// Adds a descriptor to this characteristic.
    /// </summary>
    public void AddDescriptor(GattDescriptor descriptor)
    {
        if (descriptor == null)
            throw new ArgumentNullException(nameof(descriptor));

        _descriptors.Add(descriptor);
    }

    /// <summary>
    /// Event raised when the characteristic value is read.
    /// </summary>
    public event EventHandler<CharacteristicReadEventArgs>? OnRead;

    /// <summary>
    /// Event raised when the characteristic value is written.
    /// </summary>
    public event EventHandler<CharacteristicWriteEventArgs>? OnWrite;

    /// <summary>
    /// Handles a read request.
    /// </summary>
    public async Task<byte[]> ReadValueAsync(Dictionary<string, object> options)
    {
        _logger.Debug($"Reading characteristic {UUID}, value: {BitConverter.ToString(_value).Replace("-", "")}");
        
        var args = new CharacteristicReadEventArgs { Value = _value };
        OnRead?.Invoke(this, args);
        
        await Task.CompletedTask;
        return args.Value;
    }

    /// <summary>
    /// Handles a write request.
    /// </summary>
    public async Task WriteValueAsync(byte[] value, Dictionary<string, object> options)
    {
        _logger.Debug($"Writing characteristic {UUID}, value: {BitConverter.ToString(value).Replace("-", "")}");
        
        var args = new CharacteristicWriteEventArgs { Value = value };
        OnWrite?.Invoke(this, args);
        
        _value = args.Value;
        await Task.CompletedTask;
    }

    /// <summary>
    /// Gets the D-Bus properties for this characteristic.
    /// </summary>
    public Dictionary<string, Dictionary<string, object>> GetProperties()
    {
        var props = new Dictionary<string, object>
        {
            ["UUID"] = UUID,
            ["Service"] = ServicePath,
            ["Value"] = _value,
            ["Flags"] = Flags
        };

        if (_descriptors.Count > 0)
        {
            props["Descriptors"] = _descriptors.Select(d => d.ObjectPath).ToArray();
        }

        return new Dictionary<string, Dictionary<string, object>>
        {
            ["org.bluez.GattCharacteristic1"] = props
        };
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            foreach (var descriptor in _descriptors)
            {
                descriptor.Dispose();
            }
            _descriptors.Clear();
            _disposed = true;
        }
    }
}

/// <summary>
/// Represents a GATT descriptor.
/// </summary>
public class GattDescriptor : IDisposable
{
    private readonly ObjectPath _objectPath;
    private byte[] _value;
    private bool _disposed;

    public GattDescriptor(string uuid, string[] flags, byte[]? initialValue = null, int index = 0, int charIndex = 0, int serviceIndex = 0)
    {
        UUID = uuid ?? throw new ArgumentNullException(nameof(uuid));
        Flags = flags ?? throw new ArgumentNullException(nameof(flags));
        _value = initialValue ?? Array.Empty<byte>();
        _objectPath = new ObjectPath($"/com/btsimulator/service{serviceIndex:D4}/char{charIndex:D4}/desc{index:D4}");
    }

    public string UUID { get; }
    public string[] Flags { get; }
    public ObjectPath ObjectPath => _objectPath;
    public ObjectPath CharacteristicPath { get; set; } = new ObjectPath("/");

    /// <summary>
    /// Gets or sets the current value of this descriptor.
    /// </summary>
    public byte[] Value
    {
        get => _value;
        set => _value = value ?? Array.Empty<byte>();
    }

    /// <summary>
    /// Handles a read request.
    /// </summary>
    public async Task<byte[]> ReadValueAsync(Dictionary<string, object> options)
    {
        await Task.CompletedTask;
        return _value;
    }

    /// <summary>
    /// Handles a write request.
    /// </summary>
    public async Task WriteValueAsync(byte[] value, Dictionary<string, object> options)
    {
        _value = value;
        await Task.CompletedTask;
    }

    /// <summary>
    /// Gets the D-Bus properties for this descriptor.
    /// </summary>
    public Dictionary<string, Dictionary<string, object>> GetProperties()
    {
        var props = new Dictionary<string, object>
        {
            ["UUID"] = UUID,
            ["Characteristic"] = CharacteristicPath,
            ["Value"] = _value,
            ["Flags"] = Flags
        };

        return new Dictionary<string, Dictionary<string, object>>
        {
            ["org.bluez.GattDescriptor1"] = props
        };
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
        }
    }
}

/// <summary>
/// Event args for characteristic read operations.
/// </summary>
public class CharacteristicReadEventArgs : EventArgs
{
    public byte[] Value { get; set; } = Array.Empty<byte>();
}

/// <summary>
/// Event args for characteristic write operations.
/// </summary>
public class CharacteristicWriteEventArgs : EventArgs
{
    public byte[] Value { get; set; } = Array.Empty<byte>();
}
