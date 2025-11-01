namespace BTSimulator.Core.BlueZ;

/// <summary>
/// Represents information about a Bluetooth adapter.
/// </summary>
public class AdapterInfo
{
    /// <summary>
    /// D-Bus object path of the adapter (e.g., "/org/bluez/hci0").
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// Short name of the adapter (e.g., "hci0").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// MAC address of the adapter.
    /// </summary>
    public string Address { get; set; } = string.Empty;

    /// <summary>
    /// Friendly alias/name of the adapter.
    /// </summary>
    public string Alias { get; set; } = string.Empty;

    /// <summary>
    /// Whether the adapter is powered on.
    /// </summary>
    public bool Powered { get; set; }

    /// <summary>
    /// Returns a user-friendly display string for the adapter.
    /// </summary>
    public override string ToString()
    {
        return $"{Name} ({Address}) - {Alias}";
    }
}
