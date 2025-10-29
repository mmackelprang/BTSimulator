using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BTSimulator.Core.Environment;
using BTSimulator.Core.Device;
using BTSimulator.Core.BlueZ;

namespace BTSimulator.Demo;

/// <summary>
/// Demonstration application for BTSimulator.
/// Shows how to use the environment verification, device configuration, and BlueZ integration.
/// </summary>
class Program
{
    static async Task<int> Main(string[] args)
    {
        Console.WriteLine("========================================");
        Console.WriteLine("   Bluetooth Device Simulator - Demo");
        Console.WriteLine("========================================");
        Console.WriteLine();

        // Step 1: Verify Environment
        Console.WriteLine("Step 1: Verifying Environment...");
        Console.WriteLine("─────────────────────────────────────");
        
        var verifier = new EnvironmentVerifier();
        var envResult = await verifier.VerifyEnvironment();
        
        Console.WriteLine(envResult.GetSummary());
        Console.WriteLine();

        if (!envResult.IsReady)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("⚠ Environment is not fully ready.");
            Console.WriteLine("  Some features may not work. See docs/linux-setup.md");
            Console.ResetColor();
            Console.WriteLine();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("✓ Environment is ready for Bluetooth simulation!");
            Console.ResetColor();
            Console.WriteLine();
        }

        // Step 2: Configure a Simulated Device
        Console.WriteLine("Step 2: Configuring Simulated Device...");
        Console.WriteLine("─────────────────────────────────────");
        
        var config = new DeviceConfiguration
        {
            DeviceName = "BT Simulator Demo",
            // DeviceAddress = "AA:BB:CC:DD:EE:FF" // Uncomment to set custom MAC
        };

        Console.WriteLine($"Device Name: {config.DeviceName}");
        if (config.DeviceAddress != null)
            Console.WriteLine($"Device Address: {config.DeviceAddress}");
        else
            Console.WriteLine("Device Address: (will use adapter default)");
        Console.WriteLine();

        // Step 3: Add GATT Services
        Console.WriteLine("Step 3: Adding GATT Services...");
        Console.WriteLine("─────────────────────────────────────");

        // Add Battery Service (0x180F)
        var batteryService = new GattServiceConfiguration
        {
            Uuid = "180F",  // Standard Battery Service UUID
            IsPrimary = true
        };

        // Add Battery Level Characteristic (0x2A19)
        var batteryLevel = new GattCharacteristicConfiguration
        {
            Uuid = "2A19",  // Standard Battery Level UUID
            Flags = new List<string> { "read", "notify" },
            InitialValue = new byte[] { 85 },  // 85% battery
            Description = "Battery level in percentage"
        };

        batteryService.AddCharacteristic(batteryLevel);
        config.AddService(batteryService);

        Console.WriteLine("✓ Added Battery Service (0x180F)");
        Console.WriteLine($"  └─ Battery Level Characteristic (0x2A19)");
        Console.WriteLine($"     - Flags: {string.Join(", ", batteryLevel.Flags)}");
        Console.WriteLine($"     - Initial Value: {batteryLevel.InitialValue[0]}%");
        Console.WriteLine();

        // Add Device Information Service (0x180A)
        var deviceInfoService = new GattServiceConfiguration
        {
            Uuid = "180A",  // Standard Device Information Service UUID
            IsPrimary = true
        };

        // Manufacturer Name
        var manufacturerName = new GattCharacteristicConfiguration
        {
            Uuid = "2A29",  // Manufacturer Name String UUID
            Flags = new List<string> { "read" },
            InitialValue = System.Text.Encoding.UTF8.GetBytes("BTSimulator"),
            Description = "Manufacturer name"
        };

        // Model Number
        var modelNumber = new GattCharacteristicConfiguration
        {
            Uuid = "2A24",  // Model Number String UUID
            Flags = new List<string> { "read" },
            InitialValue = System.Text.Encoding.UTF8.GetBytes("Demo-v1.0"),
            Description = "Model number"
        };

        deviceInfoService.AddCharacteristic(manufacturerName);
        deviceInfoService.AddCharacteristic(modelNumber);
        config.AddService(deviceInfoService);

        Console.WriteLine("✓ Added Device Information Service (0x180A)");
        Console.WriteLine("  ├─ Manufacturer Name Characteristic (0x2A29)");
        Console.WriteLine($"     - Value: {System.Text.Encoding.UTF8.GetString(manufacturerName.InitialValue)}");
        Console.WriteLine("  └─ Model Number Characteristic (0x2A24)");
        Console.WriteLine($"     - Value: {System.Text.Encoding.UTF8.GetString(modelNumber.InitialValue)}");
        Console.WriteLine();

        // Step 4: Validate Configuration
        Console.WriteLine("Step 4: Validating Configuration...");
        Console.WriteLine("─────────────────────────────────────");
        
        if (config.Validate(out var errors))
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("✓ Configuration is valid!");
            Console.ResetColor();
            Console.WriteLine();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("✗ Configuration has errors:");
            foreach (var error in errors)
            {
                Console.WriteLine($"  - {error}");
            }
            Console.ResetColor();
            Console.WriteLine();
            return 1;
        }

        // Step 5: Connect to BlueZ (if available)
        Console.WriteLine("Step 5: Connecting to BlueZ...");
        Console.WriteLine("─────────────────────────────────────");

        if (!envResult.IsLinux)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("⚠ Not running on Linux - BlueZ connection skipped");
            Console.ResetColor();
            Console.WriteLine();
        }
        else if (!envResult.BlueZResult.IsInstalled)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("⚠ BlueZ not installed - connection skipped");
            Console.WriteLine("  Install with: sudo apt-get install bluez");
            Console.ResetColor();
            Console.WriteLine();
        }
        else
        {
            try
            {
                var manager = new BlueZManager();
                var connected = await manager.ConnectAsync();

                if (connected)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("✓ Connected to BlueZ via D-Bus");
                    Console.ResetColor();

                    // Try to get default adapter
                    try
                    {
                        var adapterPath = await manager.GetDefaultAdapterAsync();
                        Console.WriteLine($"  Default Adapter: {adapterPath}");
                        Console.WriteLine();

                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine("Note: Full D-Bus property access is in development (Phase 2).");
                        Console.WriteLine("      Adapter operations will be available in the next release.");
                        Console.ResetColor();
                    }
                    catch (Exception ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"  Note: {ex.Message}");
                        Console.ResetColor();
                    }
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("✗ Failed to connect to BlueZ");
                    Console.ResetColor();
                }
                
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"✗ Error connecting to BlueZ: {ex.Message}");
                Console.ResetColor();
                Console.WriteLine();
            }
        }

        // Summary
        Console.WriteLine("========================================");
        Console.WriteLine("Summary");
        Console.WriteLine("========================================");
        Console.WriteLine($"Platform: {(envResult.IsLinux ? (envResult.IsWSL2 ? "WSL2" : "Linux") : "Other")}");
        Console.WriteLine($"BlueZ: {(envResult.BlueZResult.IsInstalled ? "Installed" : "Not Installed")}");
        Console.WriteLine($"Services Configured: {config.Services.Count}");
        
        var totalCharacteristics = config.Services.Sum(s => s.Characteristics.Count);
        Console.WriteLine($"Characteristics Configured: {totalCharacteristics}");
        Console.WriteLine();

        Console.WriteLine("Next Steps:");
        Console.WriteLine("1. Review the configuration in this demo");
        Console.WriteLine("2. Check docs/linux-setup.md for environment setup");
        Console.WriteLine("3. See docs/api-mapping.md for BlueZ integration details");
        Console.WriteLine("4. Run ./scripts/verify-environment.sh for detailed checks");
        Console.WriteLine();

        Console.WriteLine("For more information, see README.md");
        Console.WriteLine();

        return 0;
    }
}
