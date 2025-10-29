using System.Collections.Generic;

namespace BTSimulator.Scanner;

/// <summary>
/// Known Bluetooth service and characteristic UUIDs with human-readable names
/// Reference: https://www.bluetooth.com/specifications/assigned-numbers/
/// </summary>
public static class KnownBluetoothServices
{
    private static readonly Dictionary<string, string> Services = new()
    {
        // Standard GATT Services (16-bit UUIDs)
        { "1800", "Generic Access" },
        { "1801", "Generic Attribute" },
        { "1802", "Immediate Alert" },
        { "1803", "Link Loss" },
        { "1804", "Tx Power" },
        { "1805", "Current Time" },
        { "1806", "Reference Time Update" },
        { "1807", "Next DST Change" },
        { "1808", "Glucose" },
        { "1809", "Health Thermometer" },
        { "180A", "Device Information" },
        { "180D", "Heart Rate" },
        { "180E", "Phone Alert Status" },
        { "180F", "Battery Service" },
        { "1810", "Blood Pressure" },
        { "1811", "Alert Notification" },
        { "1812", "Human Interface Device" },
        { "1813", "Scan Parameters" },
        { "1814", "Running Speed and Cadence" },
        { "1815", "Automation IO" },
        { "1816", "Cycling Speed and Cadence" },
        { "1818", "Cycling Power" },
        { "1819", "Location and Navigation" },
        { "181A", "Environmental Sensing" },
        { "181B", "Body Composition" },
        { "181C", "User Data" },
        { "181D", "Weight Scale" },
        { "181E", "Bond Management" },
        { "181F", "Continuous Glucose Monitoring" },
        { "1820", "Internet Protocol Support" },
        { "1821", "Indoor Positioning" },
        { "1822", "Pulse Oximeter" },
        { "1823", "HTTP Proxy" },
        { "1824", "Transport Discovery" },
        { "1825", "Object Transfer" },
        { "1826", "Fitness Machine" },
        { "1827", "Mesh Provisioning" },
        { "1828", "Mesh Proxy" }
    };

    private static readonly Dictionary<string, string> Characteristics = new()
    {
        // Standard GATT Characteristics (16-bit UUIDs)
        { "2A00", "Device Name" },
        { "2A01", "Appearance" },
        { "2A02", "Peripheral Privacy Flag" },
        { "2A03", "Reconnection Address" },
        { "2A04", "Peripheral Preferred Connection Parameters" },
        { "2A05", "Service Changed" },
        { "2A06", "Alert Level" },
        { "2A07", "Tx Power Level" },
        { "2A08", "Date Time" },
        { "2A09", "Day of Week" },
        { "2A0A", "Day Date Time" },
        { "2A19", "Battery Level" },
        { "2A1C", "Temperature Measurement" },
        { "2A1E", "Intermediate Temperature" },
        { "2A23", "System ID" },
        { "2A24", "Model Number String" },
        { "2A25", "Serial Number String" },
        { "2A26", "Firmware Revision String" },
        { "2A27", "Hardware Revision String" },
        { "2A28", "Software Revision String" },
        { "2A29", "Manufacturer Name String" },
        { "2A2A", "IEEE 11073-20601 Regulatory Certification Data List" },
        { "2A2B", "Current Time" },
        { "2A31", "Scan Refresh" },
        { "2A35", "Blood Pressure Measurement" },
        { "2A37", "Heart Rate Measurement" },
        { "2A38", "Body Sensor Location" },
        { "2A39", "Heart Rate Control Point" },
        { "2A3F", "Alert Status" },
        { "2A46", "New Alert" },
        { "2A47", "Supported New Alert Category" },
        { "2A48", "Supported Unread Alert Category" },
        { "2A49", "Blood Pressure Feature" },
        { "2A4A", "HID Information" },
        { "2A4B", "Report Map" },
        { "2A4C", "HID Control Point" },
        { "2A4D", "Report" },
        { "2A4E", "Protocol Mode" },
        { "2A4F", "Scan Interval Window" },
        { "2A50", "PnP ID" },
        { "2A51", "Glucose Feature" },
        { "2A52", "Record Access Control Point" },
        { "2A53", "RSC Measurement" },
        { "2A54", "RSC Feature" },
        { "2A55", "SC Control Point" },
        { "2A56", "Digital" },
        { "2A58", "Analog" },
        { "2A5A", "Aggregate" },
        { "2A5B", "CSC Measurement" },
        { "2A5C", "CSC Feature" },
        { "2A5D", "Sensor Location" },
        { "2A63", "Cycling Power Measurement" },
        { "2A64", "Cycling Power Vector" },
        { "2A65", "Cycling Power Feature" },
        { "2A66", "Cycling Power Control Point" },
        { "2A67", "Location and Speed" },
        { "2A68", "Navigation" },
        { "2A6C", "Elevation" },
        { "2A6D", "Pressure" },
        { "2A6E", "Temperature" },
        { "2A6F", "Humidity" },
        { "2A70", "True Wind Speed" },
        { "2A71", "True Wind Direction" },
        { "2A72", "Apparent Wind Speed" },
        { "2A73", "Apparent Wind Direction" },
        { "2A74", "Gust Factor" },
        { "2A75", "Pollen Concentration" },
        { "2A76", "UV Index" },
        { "2A77", "Irradiance" },
        { "2A78", "Rainfall" },
        { "2A79", "Wind Chill" },
        { "2A7A", "Heat Index" },
        { "2A7B", "Dew Point" }
    };

    /// <summary>
    /// Gets the human-readable name for a service UUID
    /// </summary>
    public static string GetServiceName(string uuid)
    {
        // Normalize UUID to 4 characters (16-bit) if possible
        var normalized = NormalizeUuid(uuid);
        
        if (Services.TryGetValue(normalized, out var name))
        {
            return name;
        }

        return "Unknown Service";
    }

    /// <summary>
    /// Gets the human-readable name for a characteristic UUID
    /// </summary>
    public static string GetCharacteristicName(string uuid)
    {
        // Normalize UUID to 4 characters (16-bit) if possible
        var normalized = NormalizeUuid(uuid);
        
        if (Characteristics.TryGetValue(normalized, out var name))
        {
            return name;
        }

        return "Unknown Characteristic";
    }

    private static string NormalizeUuid(string uuid)
    {
        // Remove dashes and convert to uppercase
        uuid = uuid.Replace("-", "").ToUpper();

        // If it's a standard 128-bit UUID based on Bluetooth base UUID
        // (0000xxxx-0000-1000-8000-00805F9B34FB), extract the 16-bit part
        if (uuid.Length == 32 && uuid.StartsWith("0000") && uuid.EndsWith("00001000800000805F9B34FB"))
        {
            return uuid.Substring(4, 4);
        }

        // If it's already a 16-bit UUID (4 characters), return it
        if (uuid.Length == 4)
        {
            return uuid;
        }

        // Return the original UUID if it doesn't match standard patterns
        return uuid;
    }
}
