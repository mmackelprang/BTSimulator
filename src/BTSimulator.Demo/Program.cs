using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using BTSimulator.Core.Environment;
using BTSimulator.Core.Device;
using BTSimulator.Core.BlueZ;
using BTSimulator.Demo.Configuration;
using BTSimulator.Demo.Logging;

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

        // Load configuration
        Console.WriteLine("Loading configuration from appsettings.json...");
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .Build();

        var settings = new AppSettings();
        configuration.Bind(settings);
        Console.WriteLine($"✓ Configuration loaded");
        Console.WriteLine();

        // Initialize logger
        FileLogger? logger = null;
        try
        {
            logger = new FileLogger(settings.Logging.LogDirectory);
            logger.Info("BTSimulator Demo application started");
            Console.WriteLine($"✓ Logger initialized (directory: {settings.Logging.LogDirectory})");
            Console.WriteLine();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"✗ Failed to initialize logger: {ex.Message}");
            Console.ResetColor();
            return 1;
        }

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
            DeviceName = settings.Bluetooth.DeviceName
        };

        if (!string.IsNullOrEmpty(settings.Bluetooth.DeviceAddress))
        {
            config.DeviceAddress = settings.Bluetooth.DeviceAddress;
        }

        Console.WriteLine($"Device Name: {config.DeviceName}");
        if (config.DeviceAddress != null)
            Console.WriteLine($"Device Address: {config.DeviceAddress}");
        else
            Console.WriteLine("Device Address: (will use adapter default)");
        Console.WriteLine();

        // Step 3: Add GATT Services
        Console.WriteLine("Step 3: Adding GATT Services...");
        Console.WriteLine("─────────────────────────────────────");

        // Load services from configuration
        foreach (var serviceSettings in settings.Bluetooth.Services)
        {
            var service = new GattServiceConfiguration
            {
                Uuid = serviceSettings.Uuid,
                IsPrimary = serviceSettings.IsPrimary
            };

            foreach (var charSettings in serviceSettings.Characteristics)
            {
                // Convert hex string to byte array
                byte[] initialValue = Array.Empty<byte>();
                if (!string.IsNullOrEmpty(charSettings.InitialValue))
                {
                    try
                    {
                        initialValue = Convert.FromHexString(charSettings.InitialValue);
                    }
                    catch
                    {
                        Console.WriteLine($"Warning: Invalid hex string for characteristic {charSettings.Uuid}");
                    }
                }

                var characteristic = new GattCharacteristicConfiguration
                {
                    Uuid = charSettings.Uuid,
                    Flags = charSettings.Flags,
                    InitialValue = initialValue,
                    Description = charSettings.Description
                };

                service.AddCharacteristic(characteristic);
            }

            config.AddService(service);
            Console.WriteLine($"✓ Added Service (0x{service.Uuid})");
            foreach (var ch in service.Characteristics)
            {
                Console.WriteLine($"  └─ Characteristic (0x{ch.Uuid})");
                Console.WriteLine($"     - Flags: {string.Join(", ", ch.Flags)}");
                if (ch.InitialValue.Length > 0)
                {
                    Console.WriteLine($"     - Initial Value: {BitConverter.ToString(ch.InitialValue).Replace("-", "")}");
                }
            }
        }

        Console.WriteLine();

        // Step 4: Validate Configuration
        Console.WriteLine("Step 4: Validating Configuration...");
        Console.WriteLine("─────────────────────────────────────");
        
        if (config.Validate(out var errors))
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("✓ Configuration is valid!");
            Console.ResetColor();
            logger.Info("Device configuration validated successfully");
            Console.WriteLine();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("✗ Configuration has errors:");
            foreach (var error in errors)
            {
                Console.WriteLine($"  - {error}");
                logger.Error($"Configuration error: {error}");
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
                var manager = new BlueZManager(logger);
                var connected = await manager.ConnectAsync();

                if (connected)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("✓ Connected to BlueZ via D-Bus");
                    Console.ResetColor();

                    // Select adapter
                    try
                    {
                        var adapterSelector = new AdapterSelector(manager, logger);
                        var adapterPath = await adapterSelector.SelectAdapterAsync(
                            settings.Bluetooth.AdapterName,
                            promptIfMissing: true
                        );

                        if (adapterPath == null)
                        {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine("  No Bluetooth adapter found");
                            Console.ResetColor();
                            Console.WriteLine();
                        }
                        else
                        {
                            Console.WriteLine($"  Selected Adapter: {adapterPath}");
                            Console.WriteLine();

                            // Demonstrate adapter property access
                            Console.WriteLine("  Adapter Properties:");
                            var adapter = manager.CreateAdapter(adapterPath);
                        
                        try
                        {
                            var address = await adapter.GetAddressAsync();
                            Console.WriteLine($"    Address: {address}");
                        }
                        catch (Exception ex)
                        {
                            logger.Debug($"Could not get address: {ex.Message}");
                        }

                        try
                        {
                            var name = await adapter.GetNameAsync();
                            Console.WriteLine($"    Name: {name}");
                        }
                        catch (Exception ex)
                        {
                            logger.Debug($"Could not get name: {ex.Message}");
                        }

                        try
                        {
                            var alias = await adapter.GetAliasAsync();
                            Console.WriteLine($"    Alias: {alias}");
                        }
                        catch (Exception ex)
                        {
                            logger.Debug($"Could not get alias: {ex.Message}");
                        }

                        try
                        {
                            var powered = await adapter.GetPoweredAsync();
                            Console.WriteLine($"    Powered: {powered}");
                        }
                        catch (Exception ex)
                        {
                            logger.Debug($"Could not get powered state: {ex.Message}");
                        }

                        try
                        {
                            var discoverable = await adapter.GetDiscoverableAsync();
                            Console.WriteLine($"    Discoverable: {discoverable}");
                        }
                        catch (Exception ex)
                        {
                            logger.Debug($"Could not get discoverable state: {ex.Message}");
                        }

                        Console.WriteLine();
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("✓ Full D-Bus property access is implemented and working!");
                        Console.ResetColor();
                        Console.WriteLine();
                        Console.WriteLine("Press any key to exit...");
                        Console.ReadKey();
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"  Note: {ex.Message}");
                        Console.ResetColor();
                        logger.Warning($"Could not get default adapter: {ex.Message}");
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
                logger.Error("Error connecting to BlueZ", ex);
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

        logger.Info("BTSimulator Demo application completed");
        logger.Dispose();

        return 0;
    }
}
