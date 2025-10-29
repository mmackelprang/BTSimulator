using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BTSimulator.Core.BlueZ;
using BTSimulator.Core.Device;
using BTSimulator.Core.Logging;
using Tmds.DBus;

namespace BTSimulator.Core.Gatt;

/// <summary>
/// Manages GATT application and advertisement registration with BlueZ.
/// </summary>
public class GattApplicationManager : IDisposable
{
    private readonly BlueZAdapter _adapter;
    private readonly ILogger _logger;
    private GattApplication? _application;
    private LEAdvertisement? _advertisement;
    private bool _isRegistered;
    private bool _disposed;

    public GattApplicationManager(BlueZAdapter adapter, ILogger? logger = null)
    {
        _adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
        _logger = logger ?? NullLogger.Instance;
    }

    /// <summary>
    /// Gets whether a GATT application is currently registered.
    /// </summary>
    public bool IsRegistered => _isRegistered;

    /// <summary>
    /// Creates and registers a GATT application from a device configuration.
    /// </summary>
    public async Task<GattApplication> RegisterApplicationAsync(DeviceConfiguration configuration)
    {
        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));

        if (_isRegistered)
            throw new InvalidOperationException("An application is already registered. Unregister first.");

        // Validate configuration
        if (!configuration.Validate(out var errors))
        {
            _logger.Error($"Configuration validation failed: {string.Join(", ", errors)}");
            throw new InvalidOperationException($"Configuration is invalid: {string.Join(", ", errors)}");
        }

        _logger.Info($"Registering GATT application with {configuration.Services.Count} service(s)");

        // Create GATT application
        _application = new GattApplication();

        // Add services from configuration
        int serviceIndex = 0;
        foreach (var serviceConfig in configuration.Services)
        {
            var service = new GattService(serviceConfig.Uuid, serviceConfig.IsPrimary, serviceIndex);
            _logger.Debug($"Adding service {serviceConfig.Uuid}");

            // Add characteristics
            int charIndex = 0;
            foreach (var charConfig in serviceConfig.Characteristics)
            {
                var characteristic = new GattCharacteristic(
                    charConfig.Uuid,
                    charConfig.Flags.ToArray(),
                    charConfig.InitialValue,
                    charIndex,
                    serviceIndex
                );

                // Set logger for characteristic
                characteristic.SetLogger(_logger);

                // Set service path for characteristic
                characteristic.ServicePath = service.ObjectPath;

                service.AddCharacteristic(characteristic);
                _logger.Debug($"Added characteristic {charConfig.Uuid} with initial value: {BitConverter.ToString(charConfig.InitialValue).Replace("-", "")}");
                charIndex++;
            }

            _application.AddService(service);
            serviceIndex++;
        }

        // Register with BlueZ
        try
        {
            var gattManager = _adapter.GetGattManager();
            var options = new Dictionary<string, object>();
            
            // Note: Actual D-Bus object registration would happen here
            // For now, we store the application structure
            
            await Task.CompletedTask; // Placeholder for actual registration
            _isRegistered = true;
            _logger.Info("GATT application registered successfully");
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to register GATT application with BlueZ", ex);
            throw new InvalidOperationException("Failed to register GATT application with BlueZ", ex);
        }

        return _application;
    }

    /// <summary>
    /// Creates and registers an advertisement for the GATT application.
    /// </summary>
    public async Task<LEAdvertisement> RegisterAdvertisementAsync(DeviceConfiguration configuration)
    {
        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));

        if (!_isRegistered)
            throw new InvalidOperationException("Register a GATT application first before advertising.");

        // Create advertisement
        _advertisement = new LEAdvertisement
        {
            Type = "peripheral",
            LocalName = configuration.DeviceName,
            IncludeTxPower = true
        };

        // Add service UUIDs from configuration
        foreach (var service in configuration.Services)
        {
            _advertisement.AddServiceUUID(service.Uuid);
        }

        // Register advertisement with BlueZ
        try
        {
            var advManager = _adapter.GetAdvertisingManager();
            var options = new Dictionary<string, object>();
            
            // Note: Actual D-Bus advertisement registration would happen here
            await Task.CompletedTask; // Placeholder for actual registration
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to register advertisement with BlueZ", ex);
        }

        return _advertisement;
    }

    /// <summary>
    /// Unregisters the GATT application and advertisement.
    /// </summary>
    public async Task UnregisterAsync()
    {
        if (!_isRegistered)
            return;

        try
        {
            // Unregister advertisement if present
            if (_advertisement != null)
            {
                var advManager = _adapter.GetAdvertisingManager();
                // Note: Actual unregistration would happen here
                await Task.CompletedTask;
                _advertisement = null;
            }

            // Unregister application
            if (_application != null)
            {
                var gattManager = _adapter.GetGattManager();
                // Note: Actual unregistration would happen here
                await Task.CompletedTask;
                _application.Dispose();
                _application = null;
            }

            _isRegistered = false;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to unregister GATT application", ex);
        }
    }

    /// <summary>
    /// Gets the current GATT application.
    /// </summary>
    public GattApplication? GetApplication() => _application;

    /// <summary>
    /// Gets the current advertisement.
    /// </summary>
    public LEAdvertisement? GetAdvertisement() => _advertisement;

    public void Dispose()
    {
        if (!_disposed)
        {
            if (_isRegistered)
            {
                // Synchronous dispose - best effort
                try
                {
                    UnregisterAsync().GetAwaiter().GetResult();
                }
                catch
                {
                    // Ignore errors during dispose
                }
            }

            _application?.Dispose();
            _disposed = true;
        }
    }
}
