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

    /// <summary>
    /// List of canned messages that can be sent on demand.
    /// </summary>
    public List<CannedMessageSettings> CannedMessages { get; set; } = new List<CannedMessageSettings>();

    /// <summary>
    /// Message to send automatically when a client connects.
    /// </summary>
    public ConnectionMessageSettings? ConnectionMessage { get; set; }
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

/// <summary>
/// Canned message configuration settings.
/// </summary>
public class CannedMessageSettings
{
    /// <summary>
    /// Name of the canned message.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// UUID of the characteristic to send this message to.
    /// </summary>
    public string CharacteristicUuid { get; set; } = string.Empty;

    /// <summary>
    /// Message data as a hex string (e.g., "48656C6C6F" for "Hello").
    /// </summary>
    public string Data { get; set; } = string.Empty;
}

/// <summary>
/// Connection message configuration settings.
/// Defines a message to be sent automatically when a client connects.
/// </summary>
public class ConnectionMessageSettings
{
    /// <summary>
    /// UUID of the characteristic to send this message to.
    /// Must be a notify or indicate characteristic.
    /// </summary>
    public string CharacteristicUuid { get; set; } = string.Empty;

    /// <summary>
    /// Message data as a hex string (e.g., "48656C6C6F" for "Hello").
    /// </summary>
    public string Data { get; set; } = string.Empty;

    /// <summary>
    /// Optional description of the message.
    /// </summary>
    public string Description { get; set; } = "Connection acknowledgment";
}
