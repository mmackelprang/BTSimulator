namespace BTSimulator.Demo.Configuration;

/// <summary>
/// Configuration settings for the application.
/// </summary>
public class AppSettings
{
    /// <summary>
    /// Logging configuration settings.
    /// </summary>
    public LoggingSettings Logging { get; set; } = new LoggingSettings();

    /// <summary>
    /// Bluetooth device configuration settings.
    /// </summary>
    public BluetoothSettings Bluetooth { get; set; } = new BluetoothSettings();
}

/// <summary>
/// Logging configuration settings.
/// </summary>
public class LoggingSettings
{
    /// <summary>
    /// Directory where log files will be stored.
    /// </summary>
    public string LogDirectory { get; set; } = "logs";

    /// <summary>
    /// Minimum log level to write. Options: Debug, Info, Warning, Error.
    /// </summary>
    public string MinLevel { get; set; } = "Info";
}

/// <summary>
/// Bluetooth device configuration settings.
/// </summary>
public class BluetoothSettings
{
    /// <summary>
    /// Name of the Bluetooth adapter to use (e.g., "hci0", "hci1", or "/org/bluez/hci0").
    /// If not specified, the default adapter will be used (or user will be prompted if multiple exist).
    /// </summary>
    public string? AdapterName { get; set; }

    /// <summary>
    /// Name of the simulated device.
    /// </summary>
    public string DeviceName { get; set; } = "BT Simulator";

    /// <summary>
    /// MAC address of the simulated device (optional).
    /// </summary>
    public string? DeviceAddress { get; set; }

    /// <summary>
    /// List of GATT services to advertise.
    /// </summary>
    public List<GattServiceSettings> Services { get; set; } = new List<GattServiceSettings>();
}

/// <summary>
/// GATT service configuration settings.
/// </summary>
public class GattServiceSettings
{
    /// <summary>
    /// Service UUID (16-bit or 128-bit).
    /// </summary>
    public string Uuid { get; set; } = string.Empty;

    /// <summary>
    /// Whether this is a primary service.
    /// </summary>
    public bool IsPrimary { get; set; } = true;

    /// <summary>
    /// List of characteristics in this service.
    /// </summary>
    public List<GattCharacteristicSettings> Characteristics { get; set; } = new List<GattCharacteristicSettings>();
}

/// <summary>
/// GATT characteristic configuration settings.
/// </summary>
public class GattCharacteristicSettings
{
    /// <summary>
    /// Characteristic UUID (16-bit or 128-bit).
    /// </summary>
    public string Uuid { get; set; } = string.Empty;

    /// <summary>
    /// Characteristic flags (e.g., "read", "write", "notify").
    /// </summary>
    public List<string> Flags { get; set; } = new List<string>();

    /// <summary>
    /// Initial value as a hex string (e.g., "55" for 85 decimal, "48656C6C6F" for "Hello").
    /// </summary>
    public string InitialValue { get; set; } = string.Empty;

    /// <summary>
    /// Description of the characteristic.
    /// </summary>
    public string Description { get; set; } = string.Empty;
}
