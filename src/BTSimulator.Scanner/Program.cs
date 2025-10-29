using System.Text;
using System.Text.Json;
using BTSimulator.Core.BlueZ;
using BTSimulator.Core.Logging;
using BTSimulator.Scanner;

Console.WriteLine("BTScanner - Bluetooth Device Discovery Utility");
Console.WriteLine("===============================================");
Console.WriteLine();

// Parse command line arguments
var scanDuration = 10; // Default scan duration in seconds
var outputFormat = "json"; // Default output format

if (args.Length > 0 && int.TryParse(args[0], out var duration))
{
    scanDuration = duration;
}

if (args.Length > 1 && (args[1].ToLower() == "text" || args[1].ToLower() == "json"))
{
    outputFormat = args[1].ToLower();
}

// Create logger (output to console only for errors)
var logger = new ConsoleLogger();

try
{
    // Create BlueZ manager
    var manager = new BlueZManager(logger);
    
    Console.WriteLine("Connecting to BlueZ...");
    if (!await manager.ConnectAsync())
    {
        Console.WriteLine("Error: Failed to connect to BlueZ. Make sure BlueZ is installed and running.");
        Console.WriteLine("Run 'bluetoothctl' to verify Bluetooth is available.");
        return 1;
    }

    // Get default adapter
    var adapterPath = await manager.GetDefaultAdapterAsync();
    if (adapterPath == null)
    {
        Console.WriteLine("Error: No Bluetooth adapter found.");
        Console.WriteLine("Make sure your Bluetooth adapter is connected and enabled.");
        return 1;
    }

    Console.WriteLine($"Using adapter: {adapterPath}");
    var adapter = manager.CreateAdapter(adapterPath);

    // Power on the adapter if needed
    var isPowered = await adapter.GetPoweredAsync();
    if (!isPowered)
    {
        Console.WriteLine("Powering on Bluetooth adapter...");
        await adapter.SetPoweredAsync(true);
        await Task.Delay(1000); // Wait for adapter to power on
    }

    // Create device scanner
    var scanner = new DeviceScanner(manager, adapter, logger);

    Console.WriteLine($"Scanning for Bluetooth devices for {scanDuration} seconds...");
    Console.WriteLine("Press Ctrl+C to stop scanning early.");
    Console.WriteLine();

    // Scan for devices
    var devices = await scanner.ScanForDevicesAsync(scanDuration);

    Console.WriteLine($"\nFound {devices.Count} device(s)");
    Console.WriteLine();

    if (devices.Count == 0)
    {
        Console.WriteLine("No devices found. Make sure Bluetooth devices are in range and advertising.");
        return 0;
    }

    // Output devices based on format
    if (outputFormat == "json")
    {
        OutputDevicesAsJson(devices);
    }
    else
    {
        OutputDevicesAsText(devices);
    }

    return 0;
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
    if (ex.InnerException != null)
    {
        Console.WriteLine($"Details: {ex.InnerException.Message}");
    }
    return 1;
}

static void OutputDevicesAsText(List<ScannedDevice> devices)
{
    foreach (var device in devices)
    {
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine($"Device Name: {device.Name ?? "(No name)"}");
        Console.WriteLine($"Device Address: {device.Address}");
        Console.WriteLine($"RSSI: {device.Rssi} dBm");
        
        if (device.ServiceUuids.Any())
        {
            Console.WriteLine($"Service UUIDs:");
            foreach (var uuid in device.ServiceUuids)
            {
                var serviceName = KnownBluetoothServices.GetServiceName(uuid);
                Console.WriteLine($"  - {uuid} ({serviceName})");
            }
        }

        if (device.Services.Any())
        {
            Console.WriteLine($"\nGATT Services:");
            foreach (var service in device.Services)
            {
                var serviceName = KnownBluetoothServices.GetServiceName(service.Uuid);
                Console.WriteLine($"  Service: {service.Uuid} ({serviceName})");
                Console.WriteLine($"  Primary: {service.IsPrimary}");
                
                if (service.Characteristics.Any())
                {
                    Console.WriteLine($"  Characteristics:");
                    foreach (var characteristic in service.Characteristics)
                    {
                        var charName = KnownBluetoothServices.GetCharacteristicName(characteristic.Uuid);
                        Console.WriteLine($"    - UUID: {characteristic.Uuid} ({charName})");
                        Console.WriteLine($"      Flags: {string.Join(", ", characteristic.Flags)}");
                        if (characteristic.Value != null && characteristic.Value.Length > 0)
                        {
                            Console.WriteLine($"      Value: {BitConverter.ToString(characteristic.Value).Replace("-", "")}");
                        }
                    }
                }
                Console.WriteLine();
            }
        }

        Console.WriteLine("\nConfiguration for appsettings.json:");
        Console.WriteLine("```json");
        Console.WriteLine(GenerateAppSettingsJson(device));
        Console.WriteLine("```");
        Console.WriteLine();
    }
}

static void OutputDevicesAsJson(List<ScannedDevice> devices)
{
    var configurations = new List<object>();

    foreach (var device in devices)
    {
        configurations.Add(new
        {
            DeviceName = device.Name ?? "(No name)",
            DeviceAddress = device.Address,
            RSSI = device.Rssi,
            Services = device.Services.Select(s => new
            {
                Uuid = s.Uuid,
                IsPrimary = s.IsPrimary,
                Characteristics = s.Characteristics.Select(c => new
                {
                    Uuid = c.Uuid,
                    Flags = c.Flags,
                    InitialValue = c.Value != null && c.Value.Length > 0
                        ? BitConverter.ToString(c.Value).Replace("-", "")
                        : null,
                    Description = $"{KnownBluetoothServices.GetCharacteristicName(c.Uuid)}"
                }).ToList()
            }).ToList()
        });
    }

    var options = new JsonSerializerOptions
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    Console.WriteLine(JsonSerializer.Serialize(configurations, options));
}

static string GenerateAppSettingsJson(ScannedDevice device)
{
    var config = new
    {
        Bluetooth = new
        {
            DeviceName = device.Name ?? "(No name)",
            DeviceAddress = device.Address,
            Services = device.Services.Select(s => new
            {
                Uuid = s.Uuid,
                IsPrimary = s.IsPrimary,
                Characteristics = s.Characteristics.Select(c => new
                {
                    Uuid = c.Uuid,
                    Flags = c.Flags,
                    InitialValue = c.Value != null && c.Value.Length > 0
                        ? BitConverter.ToString(c.Value).Replace("-", "")
                        : null,
                    Description = $"{KnownBluetoothServices.GetCharacteristicName(c.Uuid)}"
                }).ToList()
            }).ToList()
        }
    };

    var options = new JsonSerializerOptions
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    return JsonSerializer.Serialize(config, options);
}
