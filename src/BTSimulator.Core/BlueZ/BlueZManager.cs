using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tmds.DBus;
using BTSimulator.Core.Logging;

namespace BTSimulator.Core.BlueZ;

/// <summary>
/// Manages connection to the BlueZ D-Bus service and provides access to adapters.
/// This class handles the communication with the BlueZ daemon via D-Bus.
/// 
/// Known Limitations:
/// - Requires BlueZ 5.x or later
/// - Needs appropriate D-Bus permissions (system bus access)
/// - WSL2 requires USB passthrough for Bluetooth adapter
/// </summary>
public class BlueZManager : IDisposable
{
    private IConnection? _connection;
    private bool _disposed;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the BlueZManager.
    /// </summary>
    /// <param name="logger">Logger instance for logging operations. If null, NullLogger is used.</param>
    public BlueZManager(ILogger? logger = null)
    {
        _logger = logger ?? NullLogger.Instance;
    }

    /// <summary>
    /// Connects to the BlueZ service via D-Bus system bus.
    /// </summary>
    /// <returns>True if connection was successful, false otherwise.</returns>
    public async Task<bool> ConnectAsync()
    {
        try
        {
            _logger.Info("Connecting to BlueZ via D-Bus system bus");
            _connection = Connection.System;
            // Test connection by checking if BlueZ service is available
            bool isAvailable = await IsBlueZAvailableAsync();
            
            if (isAvailable)
            {
                _logger.Info("Successfully connected to BlueZ");
            }
            else
            {
                _logger.Warning("BlueZ service is not available");
            }
            
            return isAvailable;
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to connect to BlueZ", ex);
            return false;
        }
    }

    /// <summary>
    /// Discovers all available Bluetooth adapters on the system.
    /// Uses ObjectManager to enumerate BlueZ objects.
    /// </summary>
    /// <returns>List of adapter object paths.</returns>
    public async Task<List<string>> GetAdaptersAsync()
    {
        if (_connection == null)
            throw new InvalidOperationException("Not connected to D-Bus. Call ConnectAsync first.");

        var adapters = new List<string>();

        try
        {
            _logger.Debug("Discovering Bluetooth adapters");
            // Use ObjectManager to discover all BlueZ objects
            var objectManager = _connection.CreateProxy<IObjectManager>(BlueZConstants.Service, "/");
            var objects = await objectManager.GetManagedObjectsAsync();

            // Filter objects that have Adapter1 interface
            foreach (var obj in objects)
            {
                if (obj.Value.ContainsKey(BlueZConstants.Adapter1Interface))
                {
                    adapters.Add(obj.Key.ToString());
                    _logger.Debug($"Found adapter: {obj.Key}");
                }
            }
            
            _logger.Info($"Discovered {adapters.Count} adapter(s)");
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to discover adapters", ex);
            throw new BlueZException("Failed to discover adapters", ex);
        }

        return adapters;
    }

    /// <summary>
    /// Gets the default (first available) Bluetooth adapter.
    /// </summary>
    /// <returns>Path to the default adapter, or null if none found.</returns>
    public async Task<string?> GetDefaultAdapterAsync()
    {
        var adapters = await GetAdaptersAsync();
        return adapters.FirstOrDefault();
    }

    /// <summary>
    /// Gets detailed information about all available Bluetooth adapters.
    /// </summary>
    /// <returns>List of adapter information.</returns>
    public async Task<List<AdapterInfo>> GetAdapterInfosAsync()
    {
        if (_connection == null)
            throw new InvalidOperationException("Not connected to D-Bus. Call ConnectAsync first.");

        var adapterInfos = new List<AdapterInfo>();
        var adapterPaths = await GetAdaptersAsync();

        foreach (var adapterPath in adapterPaths)
        {
            try
            {
                var adapter = CreateAdapter(adapterPath);
                var info = new AdapterInfo
                {
                    Path = adapterPath,
                    Name = ExtractAdapterName(adapterPath)
                };

                try
                {
                    info.Address = await adapter.GetAddressAsync();
                }
                catch (Exception ex)
                {
                    _logger.Debug($"Could not get address for {adapterPath}: {ex.Message}");
                    info.Address = "Unknown";
                }

                try
                {
                    info.Alias = await adapter.GetAliasAsync();
                }
                catch (Exception ex)
                {
                    _logger.Debug($"Could not get alias for {adapterPath}: {ex.Message}");
                    info.Alias = info.Name;
                }

                try
                {
                    info.Powered = await adapter.GetPoweredAsync();
                }
                catch (Exception ex)
                {
                    _logger.Debug($"Could not get powered state for {adapterPath}: {ex.Message}");
                    info.Powered = false;
                }

                adapterInfos.Add(info);
            }
            catch (Exception ex)
            {
                _logger.Warning($"Failed to get info for adapter {adapterPath}: {ex.Message}");
            }
        }

        return adapterInfos;
    }

    /// <summary>
    /// Extracts the adapter name (e.g., "hci0") from the full D-Bus path.
    /// </summary>
    private static string ExtractAdapterName(string adapterPath)
    {
        if (string.IsNullOrEmpty(adapterPath))
            return adapterPath;
            
        var parts = adapterPath.Split('/');
        return parts.Length > 0 && !string.IsNullOrEmpty(parts[^1]) ? parts[^1] : adapterPath;
    }

    /// <summary>
    /// Creates a proxy to access a specific Bluetooth adapter.
    /// </summary>
    /// <param name="adapterPath">D-Bus object path of the adapter (e.g., "/org/bluez/hci0").</param>
    /// <returns>Adapter proxy for operations.</returns>
    public BlueZAdapter CreateAdapter(string adapterPath)
    {
        if (_connection == null)
            throw new InvalidOperationException("Not connected to D-Bus. Call ConnectAsync first.");

        _logger.Debug($"Creating adapter proxy for {adapterPath}");
        return new BlueZAdapter(_connection, adapterPath, _logger);
    }

    /// <summary>
    /// Checks if the BlueZ service is available on D-Bus.
    /// </summary>
    /// <returns>True if BlueZ service is running and accessible.</returns>
    public async Task<bool> IsBlueZAvailableAsync()
    {
        if (_connection == null)
            return false;

        try
        {
            // Try to call GetManagedObjects to verify BlueZ is available
            var objectManager = _connection.CreateProxy<IObjectManager>(BlueZConstants.Service, "/");
            await objectManager.GetManagedObjectsAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets the underlying D-Bus connection.
    /// </summary>
    /// <returns>The D-Bus connection instance.</returns>
    public IConnection GetConnection()
    {
        if (_connection == null)
            throw new InvalidOperationException("Not connected to D-Bus. Call ConnectAsync first.");
        
        return _connection;
    }

    /// <summary>
    /// Gets the ObjectManager proxy for BlueZ.
    /// </summary>
    /// <returns>ObjectManager proxy for monitoring D-Bus objects.</returns>
    public IObjectManager GetObjectManager()
    {
        if (_connection == null)
            throw new InvalidOperationException("Not connected to D-Bus. Call ConnectAsync first.");
        
        return _connection.CreateProxy<IObjectManager>(BlueZConstants.Service, "/");
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            // Connection.System is a static instance, no need to dispose
            _disposed = true;
        }
    }
}

/// <summary>
/// Represents a BlueZ Bluetooth adapter with methods for configuration and advertising.
/// </summary>
public class BlueZAdapter
{
    private readonly IConnection _connection;
    private readonly string _adapterPath;
    private readonly IAdapter1 _adapter;
    private readonly ILogger _logger;

    internal BlueZAdapter(IConnection connection, string adapterPath, ILogger logger)
    {
        _connection = connection;
        _adapterPath = adapterPath;
        _adapter = connection.CreateProxy<IAdapter1>(BlueZConstants.Service, adapterPath);
        _logger = logger;
    }

    /// <summary>
    /// Gets all properties from the adapter.
    /// </summary>
    public async Task<Adapter1Properties> GetAllPropertiesAsync()
    {
        try
        {
            var objectManager = _connection.CreateProxy<IObjectManager>(BlueZConstants.Service, "/");
            var objects = await objectManager.GetManagedObjectsAsync();
            if (objects.TryGetValue(new ObjectPath(_adapterPath), out var interfaces) &&
                interfaces.TryGetValue(BlueZConstants.Adapter1Interface, out var dict))
            {
                return Adapter1Properties.FromDictionary(dict);
            }

            throw new BlueZException($"Adapter properties not found for {_adapterPath}");
        }
        catch (BlueZException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new BlueZException("Failed to get adapter properties", ex);
        }
    }

    /// <summary>
    /// Gets the adapter's Bluetooth MAC address.
    /// </summary>
    public async Task<string> GetAddressAsync()
    {
        try
        {
            return await _adapter.GetAsync<string>("Address");
        }
        catch (Exception ex)
        {
            throw new BlueZException("Failed to get adapter address", ex);
        }
    }

    /// <summary>
    /// Gets the adapter's friendly name.
    /// </summary>
    public async Task<string> GetNameAsync()
    {
        try
        {
            return await _adapter.GetAsync<string>("Name");
        }
        catch (Exception ex)
        {
            throw new BlueZException("Failed to get adapter name", ex);
        }
    }

    /// <summary>
    /// Gets the adapter's alias (advertised name).
    /// </summary>
    public async Task<string> GetAliasAsync()
    {
        try
        {
            return await _adapter.GetAsync<string>("Alias");
        }
        catch (Exception ex)
        {
            throw new BlueZException("Failed to get adapter alias", ex);
        }
    }

    /// <summary>
    /// Sets the adapter's alias (advertised name).
    /// Note: This affects the name that other devices see during scanning.
    /// </summary>
    public async Task SetAliasAsync(string alias)
    {
        try
        {
            await _adapter.SetAsync("Alias", alias);
        }
        catch (Exception ex)
        {
            throw new BlueZException("Failed to set adapter alias", ex);
        }
    }

    /// <summary>
    /// Gets the current power state of the adapter.
    /// </summary>
    public async Task<bool> GetPoweredAsync()
    {
        try
        {
            return await _adapter.GetAsync<bool>("Powered");
        }
        catch (Exception ex)
        {
            throw new BlueZException("Failed to get powered state "+ ex.Message, ex);
        }
    }

    /// <summary>
    /// Powers the adapter on or off.
    /// </summary>
    public async Task SetPoweredAsync(bool powered)
    {
        try
        {
            await _adapter.SetAsync("Powered", powered);
        }
        catch (Exception ex)
        {
            throw new BlueZException("Failed to set powered state", ex);
        }
    }

    /// <summary>
    /// Gets whether the adapter is discoverable by other devices.
    /// </summary>
    public async Task<bool> GetDiscoverableAsync()
    {
        try
        {
            return await _adapter.GetAsync<bool>("Discoverable");
        }
        catch (Exception ex)
        {
            throw new BlueZException("Failed to get discoverable state", ex);
        }
    }

    /// <summary>
    /// Sets whether the adapter is discoverable by other devices.
    /// </summary>
    public async Task SetDiscoverableAsync(bool discoverable)
    {
        try
        {
            await _adapter.SetAsync("Discoverable", discoverable);
        }
        catch (Exception ex)
        {
            throw new BlueZException("Failed to set discoverable state", ex);
        }
    }

    /// <summary>
    /// Gets whether the adapter is currently discovering.
    /// </summary>
    public async Task<bool> GetDiscoveringAsync()
    {
        try
        {
            return await _adapter.GetAsync<bool>("Discovering");
        }
        catch (Exception ex)
        {
            throw new BlueZException("Failed to get discovering state", ex);
        }
    }

    /// <summary>
    /// Gets the list of UUIDs supported by this adapter.
    /// </summary>
    public async Task<string[]> GetUuidsAsync()
    {
        try
        {
            return await _adapter.GetAsync<string[]>("UUIDs");
        }
        catch (Exception ex)
        {
            throw new BlueZException("Failed to get adapter UUIDs", ex);
        }
    }

    /// <summary>
    /// Starts device discovery.
    /// </summary>
    public async Task StartDiscoveryAsync()
    {
        try
        {
            await _adapter.StartDiscoveryAsync();
        }
        catch (Exception ex)
        {
            throw new BlueZException("Failed to start discovery", ex);
        }
    }

    /// <summary>
    /// Stops device discovery.
    /// </summary>
    public async Task StopDiscoveryAsync()
    {
        try
        {
            await _adapter.StopDiscoveryAsync();
        }
        catch (Exception ex)
        {
            throw new BlueZException("Failed to stop discovery", ex);
        }
    }

    /// <summary>
    /// Gets the LEAdvertisingManager1 proxy for this adapter.
    /// </summary>
    public ILEAdvertisingManager1 GetAdvertisingManager()
    {
        return _connection.CreateProxy<ILEAdvertisingManager1>(BlueZConstants.Service, _adapterPath);
    }

    /// <summary>
    /// Gets the GattManager1 proxy for this adapter.
    /// </summary>
    public IGattManager1 GetGattManager()
    {
        return _connection.CreateProxy<IGattManager1>(BlueZConstants.Service, _adapterPath);
    }

    public string AdapterPath => _adapterPath;
}

/// <summary>
/// Exception thrown for BlueZ-specific errors.
/// </summary>
public class BlueZException : Exception
{
    public BlueZException(string message) : base(message) { }
    public BlueZException(string message, Exception innerException) : base(message, innerException) { }
}
