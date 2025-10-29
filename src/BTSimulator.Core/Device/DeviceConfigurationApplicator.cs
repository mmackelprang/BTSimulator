using System;
using System.Threading.Tasks;
using BTSimulator.Core.BlueZ;

namespace BTSimulator.Core.Device;

/// <summary>
/// Applies device configuration to a BlueZ adapter at runtime.
/// </summary>
public class DeviceConfigurationApplicator
{
    private readonly BlueZAdapter _adapter;

    public DeviceConfigurationApplicator(BlueZAdapter adapter)
    {
        _adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
    }

    /// <summary>
    /// Applies device configuration to the adapter.
    /// </summary>
    /// <param name="configuration">The configuration to apply.</param>
    /// <returns>True if all configurations were applied successfully.</returns>
    public async Task<ConfigurationApplicationResult> ApplyConfigurationAsync(DeviceConfiguration configuration)
    {
        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));

        // Validate configuration first
        if (!configuration.Validate(out var errors))
        {
            return new ConfigurationApplicationResult
            {
                Success = false,
                Errors = errors
            };
        }

        var result = new ConfigurationApplicationResult { Success = true };

        try
        {
            // Apply device name (alias)
            await ApplyDeviceNameAsync(configuration.DeviceName);
            result.AppliedSettings.Add("DeviceName");

            // Note: MAC address cannot be changed at runtime in most cases
            // This would require adapter-specific tools and elevated privileges
            if (!string.IsNullOrEmpty(configuration.DeviceAddress))
            {
                result.Warnings.Add("MAC address modification is not supported at runtime. The configured address will be ignored.");
            }

            // GATT services will be registered in Phase 4
            if (configuration.Services.Count > 0)
            {
                result.Warnings.Add($"GATT service registration will be implemented in Phase 4. {configuration.Services.Count} service(s) configured but not yet applied.");
            }
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Errors.Add($"Failed to apply configuration: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// Applies the device name to the adapter.
    /// </summary>
    private async Task ApplyDeviceNameAsync(string deviceName)
    {
        try
        {
            // Set the adapter alias, which is the name other devices see
            await _adapter.SetAliasAsync(deviceName);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to set device name: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Gets the current configuration from the adapter.
    /// </summary>
    /// <returns>Current device configuration.</returns>
    public async Task<DeviceConfiguration> GetCurrentConfigurationAsync()
    {
        var configuration = new DeviceConfiguration();

        try
        {
            // Get current alias (device name)
            configuration.DeviceName = await _adapter.GetAliasAsync();

            // Get MAC address (read-only)
            configuration.DeviceAddress = await _adapter.GetAddressAsync();

            // GATT services will be read in Phase 4
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to read current configuration: {ex.Message}", ex);
        }

        return configuration;
    }
}

/// <summary>
/// Result of applying a device configuration.
/// </summary>
public class ConfigurationApplicationResult
{
    public bool Success { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public List<string> AppliedSettings { get; set; } = new();
}
