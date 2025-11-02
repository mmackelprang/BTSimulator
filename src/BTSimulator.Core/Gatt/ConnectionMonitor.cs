using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tmds.DBus;
using BTSimulator.Core.BlueZ;
using BTSimulator.Core.Logging;

namespace BTSimulator.Core.Gatt;

/// <summary>
/// Monitors device connections via BlueZ D-Bus signals.
/// Tracks when clients connect to the simulated peripheral device.
/// </summary>
public class ConnectionMonitor : IDisposable
{
    private readonly BlueZManager _manager;
    private readonly ILogger _logger;
    private readonly List<string> _connectedDevices = new();
    private IDisposable? _interfacesAddedWatcher;
    private IDisposable? _propertyWatcher;
    private bool _disposed;
    private bool _isMonitoring;

    /// <summary>
    /// Event raised when a device connects to the simulated peripheral.
    /// </summary>
    public event EventHandler<DeviceConnectionEventArgs>? DeviceConnected;

    /// <summary>
    /// Event raised when a device disconnects from the simulated peripheral.
    /// </summary>
    public event EventHandler<DeviceConnectionEventArgs>? DeviceDisconnected;

    public ConnectionMonitor(BlueZManager manager, ILogger logger)
    {
        _manager = manager ?? throw new ArgumentNullException(nameof(manager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets whether the monitor is currently watching for connections.
    /// </summary>
    public bool IsMonitoring => _isMonitoring;

    /// <summary>
    /// Gets the list of currently connected device addresses.
    /// </summary>
    public IReadOnlyList<string> ConnectedDevices => _connectedDevices.AsReadOnly();

    /// <summary>
    /// Starts monitoring for device connections.
    /// </summary>
    public async Task StartMonitoringAsync()
    {
        if (_isMonitoring)
        {
            _logger.Warning("Connection monitoring is already started");
            return;
        }

        try
        {
            _logger.Info("Starting connection monitoring");

            // Watch for new devices being added (InterfacesAdded signal)
            var objectManager = _manager.GetObjectManager();
            _interfacesAddedWatcher = await objectManager.WatchInterfacesAddedAsync(
                args => OnInterfacesAdded(args.objectPath, args.interfaces)
            );

            _isMonitoring = true;
            _logger.Info("Connection monitoring started successfully");
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to start connection monitoring", ex);
            throw;
        }
    }

    /// <summary>
    /// Stops monitoring for device connections.
    /// </summary>
    public void StopMonitoring()
    {
        if (!_isMonitoring)
        {
            return;
        }

        _logger.Info("Stopping connection monitoring");
        
        _interfacesAddedWatcher?.Dispose();
        _interfacesAddedWatcher = null;
        _propertyWatcher?.Dispose();
        _propertyWatcher = null;

        _isMonitoring = false;
        _logger.Info("Connection monitoring stopped");
    }

    private void OnInterfacesAdded(ObjectPath objectPath, IDictionary<string, IDictionary<string, object>> interfaces)
    {
        try
        {
            // Check if this is a Device1 interface being added
            if (interfaces.ContainsKey("org.bluez.Device1"))
            {
                var deviceProps = interfaces["org.bluez.Device1"];
                var device = Device1Properties.FromDictionary(deviceProps);

                _logger.Debug($"Device added: {device.Address}, Connected: {device.Connected}");

                // If device is already connected when discovered
                if (device.Connected)
                {
                    HandleDeviceConnected(device.Address, objectPath);
                }
                else
                {
                    // Watch for property changes on this device to catch when it connects
                    WatchDeviceProperties(objectPath);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Error handling InterfacesAdded for {objectPath}", ex);
        }
    }

    private async void WatchDeviceProperties(ObjectPath devicePath)
    {
        try
        {
            var device = _manager.GetConnection().CreateProxy<IDevice1>("org.bluez", devicePath);
            
            await device.WatchPropertiesAsync(changes =>
            {
                try
                {
                    // PropertyChanges.Changed is an array of KeyValuePair<string, object>
                    // We need to iterate through it to find the "Connected" property
                    foreach (var change in changes.Changed)
                    {
                        if (change.Key == "Connected")
                        {
                            bool isConnected = Convert.ToBoolean(change.Value);
                            
                            // Get device address
                            var addressTask = device.GetAsync<string>("Address");
                            addressTask.Wait();
                            string address = addressTask.Result;

                            _logger.Debug($"Device {address} connection state changed: {isConnected}");

                            if (isConnected)
                            {
                                HandleDeviceConnected(address, devicePath);
                            }
                            else
                            {
                                HandleDeviceDisconnected(address, devicePath);
                            }
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error($"Error handling property change for {devicePath}", ex);
                }
            });
        }
        catch (Exception ex)
        {
            _logger.Error($"Error watching device properties for {devicePath}", ex);
        }
    }

    private void HandleDeviceConnected(string deviceAddress, ObjectPath devicePath)
    {
        if (!_connectedDevices.Contains(deviceAddress))
        {
            _connectedDevices.Add(deviceAddress);
            _logger.Info($"[CONNECTION] Device connected: {deviceAddress} (path: {devicePath})");
            
            var eventArgs = new DeviceConnectionEventArgs
            {
                DeviceAddress = deviceAddress,
                DevicePath = devicePath,
                Timestamp = DateTime.UtcNow
            };

            DeviceConnected?.Invoke(this, eventArgs);
        }
    }

    private void HandleDeviceDisconnected(string deviceAddress, ObjectPath devicePath)
    {
        if (_connectedDevices.Remove(deviceAddress))
        {
            _logger.Info($"[DISCONNECTION] Device disconnected: {deviceAddress} (path: {devicePath})");
            
            var eventArgs = new DeviceConnectionEventArgs
            {
                DeviceAddress = deviceAddress,
                DevicePath = devicePath,
                Timestamp = DateTime.UtcNow
            };

            DeviceDisconnected?.Invoke(this, eventArgs);
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            StopMonitoring();
            _connectedDevices.Clear();
            _disposed = true;
        }
    }
}

/// <summary>
/// Event arguments for device connection/disconnection events.
/// </summary>
public class DeviceConnectionEventArgs : EventArgs
{
    /// <summary>
    /// Gets or sets the Bluetooth address of the device.
    /// </summary>
    public string DeviceAddress { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the D-Bus object path of the device.
    /// </summary>
    public ObjectPath DevicePath { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the connection event occurred.
    /// </summary>
    public DateTime Timestamp { get; set; }
}
