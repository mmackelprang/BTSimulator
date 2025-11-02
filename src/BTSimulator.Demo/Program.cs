using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using BTSimulator.Core.Environment;
using BTSimulator.Core.Device;
using BTSimulator.Core.BlueZ;
using BTSimulator.Core.Gatt;
using BTSimulator.Demo.Configuration;
using BTSimulator.Demo.Logging;

namespace BTSimulator.Demo;

/// <summary>
/// Demonstration application for BTSimulator.
/// Shows how to use the environment verification, device configuration, and BlueZ integration.
/// </summary>
class Program
{
    private static AppSettings? _settings;
    private static FileLogger? _logger;
    private static GattApplication? _application;
    private static Dictionary<string, GattCharacteristic> _characteristicsByUuid = new();
    private static bool _isRunning = false;
    private static ConnectionMonitor? _connectionMonitor;
    private static BlueZManager? _manager;

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

        _settings = new AppSettings();
        configuration.Bind(_settings);
        Console.WriteLine($"✓ Configuration loaded");
        Console.WriteLine();

        // Initialize logger
        try
        {
            _logger = new FileLogger(_settings.Logging.LogDirectory);
            _logger.Info("BTSimulator Demo application started");
            Console.WriteLine($"✓ Logger initialized (directory: {_settings.Logging.LogDirectory})");
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
            DeviceName = _settings.Bluetooth.DeviceName
        };

        if (!string.IsNullOrEmpty(_settings.Bluetooth.DeviceAddress))
        {
            config.DeviceAddress = _settings.Bluetooth.DeviceAddress;
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
        foreach (var serviceSettings in _settings.Bluetooth.Services)
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
            _logger.Info("Device configuration validated successfully");
            Console.WriteLine();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("✗ Configuration has errors:");
            foreach (var error in errors)
            {
                Console.WriteLine($"  - {error}");
                _logger.Error($"Configuration error: {error}");
            }
            Console.ResetColor();
            Console.WriteLine();
            return 1;
        }

        // Step 5: Connect to BlueZ
        Console.WriteLine("Step 5: Connecting to BlueZ...");
        Console.WriteLine("─────────────────────────────────────");

        if (!envResult.IsLinux)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("⚠ Not running on Linux - BlueZ connection skipped");
            Console.ResetColor();
            Console.WriteLine();
            Console.WriteLine("Demo cannot start advertising without BlueZ.");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
            return 0;
        }
        else if (!envResult.BlueZResult.IsInstalled)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("⚠ BlueZ not installed - connection skipped");
            Console.WriteLine("  Install with: sudo apt-get install bluez");
            Console.ResetColor();
            Console.WriteLine();
            Console.WriteLine("Demo cannot start advertising without BlueZ.");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
            return 0;
        }

        try
        {
            _manager = new BlueZManager(_logger);
            var connected = await _manager.ConnectAsync();

            if (!connected)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("✗ Failed to connect to BlueZ");
                Console.ResetColor();
                Console.WriteLine();
                return 1;
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("✓ Connected to BlueZ via D-Bus");
            Console.ResetColor();

            // Select adapter
            var adapterSelector = new AdapterSelector(_manager, _logger);
            var adapterPath = await adapterSelector.SelectAdapterAsync(
                _settings.Bluetooth.AdapterName,
                promptIfMissing: true
            );

            if (adapterPath == null)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("  No Bluetooth adapter found");
                Console.ResetColor();
                Console.WriteLine();
                return 1;
            }

            Console.WriteLine($"  Selected Adapter: {adapterPath}");
            Console.WriteLine();

            var adapter = _manager.CreateAdapter(adapterPath);

            // Display adapter properties
            Console.WriteLine("  Adapter Properties:");
            try
            {
                var address = await adapter.GetAddressAsync();
                Console.WriteLine($"    Address: {address}");
            }
            catch (Exception ex)
            {
                _logger.Debug($"Could not get address: {ex.Message}");
            }

            try
            {
                var powered = await adapter.GetPoweredAsync();
                Console.WriteLine($"    Powered: {powered}");
                if (!powered)
                {
                    Console.WriteLine("    Note: Adapter is not powered. Attempting to power on...");
                    await adapter.SetPoweredAsync(true);
                    Console.WriteLine("    ✓ Adapter powered on");
                }
            }
            catch (Exception ex)
            {
                _logger.Debug($"Could not get/set powered state: {ex.Message}");
            }

            Console.WriteLine();

            // Step 6: Register GATT Application
            Console.WriteLine("Step 6: Registering GATT Application...");
            Console.WriteLine("─────────────────────────────────────");

            _application = CreateGattApplication(config);
            
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"✓ GATT Application created with {_application.Services.Count} service(s)");
            Console.ResetColor();
            _logger.Info($"GATT Application created with {_application.Services.Count} service(s)");
            Console.WriteLine();

            // Step 7: Start Advertising
            Console.WriteLine("Step 7: Starting Advertisement...");
            Console.WriteLine("─────────────────────────────────────");
            
            var advertisement = CreateAdvertisement(config);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("✓ Advertisement prepared");
            Console.WriteLine($"  Type: {advertisement.Type}");
            Console.WriteLine($"  Local Name: {advertisement.LocalName}");
            Console.WriteLine($"  Service UUIDs: {string.Join(", ", advertisement.ServiceUUIDs)}");
            Console.ResetColor();
            _logger.Info("Advertisement prepared");
            Console.WriteLine();

            // Step 8: Start Connection Monitoring
            Console.WriteLine("Step 8: Starting Connection Monitoring...");
            Console.WriteLine("─────────────────────────────────────");
            
            _connectionMonitor = new ConnectionMonitor(_manager, _logger);
            _connectionMonitor.DeviceConnected += OnDeviceConnected;
            _connectionMonitor.DeviceDisconnected += OnDeviceDisconnected;
            
            try
            {
                await _connectionMonitor.StartMonitoringAsync();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("✓ Connection monitoring started");
                Console.ResetColor();
                _logger.Info("Connection monitoring started successfully");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"⚠ Connection monitoring failed to start: {ex.Message}");
                Console.WriteLine("  Device will still advertise, but connection events won't be tracked");
                Console.ResetColor();
                _logger.Warning($"Connection monitoring failed to start: {ex.Message}");
            }
            Console.WriteLine();

            _isRunning = true;

            // Interactive Menu
            Console.WriteLine("========================================");
            Console.WriteLine("         Device is Now Running");
            Console.WriteLine("========================================");
            Console.WriteLine();
            await RunInteractiveMenu();

            _logger.Info("BTSimulator Demo application completed");
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"✗ Error: {ex.Message}");
            Console.ResetColor();
            _logger.Error("Error in main application", ex);
            Console.WriteLine();
            return 1;
        }
        finally
        {
            _connectionMonitor?.Dispose();
            _manager?.Dispose();
            _logger?.Dispose();
        }

        return 0;
    }

    private static GattApplication CreateGattApplication(DeviceConfiguration config)
    {
        if (_logger == null)
        {
            throw new InvalidOperationException("Logger must be initialized before creating GATT application");
        }

        var application = new GattApplication();
        int serviceIndex = 0;

        foreach (var serviceConfig in config.Services)
        {
            var service = new GattService(serviceConfig.Uuid, serviceConfig.IsPrimary, serviceIndex);
            
            int charIndex = 0;
            foreach (var charConfig in serviceConfig.Characteristics)
            {
                var characteristic = new GattCharacteristic(
                    charConfig.Uuid,
                    charConfig.Flags.ToArray(),
                    charConfig.InitialValue,
                    charIndex,
                    serviceIndex
                );

                characteristic.SetLogger(_logger);
                characteristic.ServicePath = service.ObjectPath;

                // Wire up handlers
                var logger = _logger; // Capture logger for lambda
                characteristic.OnRead += (sender, args) =>
                {
                    var ch = (GattCharacteristic)sender!;
                    logger.Info($"[READ] Characteristic {ch.UUID}: {BitConverter.ToString(args.Value).Replace("-", "")}");
                    Console.WriteLine($"[READ] {ch.UUID}: {BitConverter.ToString(args.Value).Replace("-", "")}");
                };

                characteristic.OnWrite += (sender, args) =>
                {
                    var ch = (GattCharacteristic)sender!;
                    logger.Info($"[WRITE] Characteristic {ch.UUID}: {BitConverter.ToString(args.Value).Replace("-", "")}");
                    Console.WriteLine($"[WRITE] {ch.UUID}: {BitConverter.ToString(args.Value).Replace("-", "")}");
                };

                service.AddCharacteristic(characteristic);
                _characteristicsByUuid[charConfig.Uuid] = characteristic;
                charIndex++;
            }

            application.AddService(service);
            serviceIndex++;
        }

        return application;
    }

    private static LEAdvertisement CreateAdvertisement(DeviceConfiguration config)
    {
        var advertisement = new LEAdvertisement
        {
            Type = "peripheral",
            LocalName = config.DeviceName,
            IncludeTxPower = true
        };

        foreach (var service in config.Services)
        {
            advertisement.AddServiceUUID(service.Uuid);
        }

        return advertisement;
    }

    private static async Task RunInteractiveMenu()
    {
        while (_isRunning)
        {
            Console.WriteLine();
            Console.WriteLine("Interactive Menu:");
            Console.WriteLine("─────────────────────────────────────");
            Console.WriteLine("1. Send Canned Message");
            Console.WriteLine("2. List Characteristics");
            Console.WriteLine("3. View Logs");
            Console.WriteLine("4. Exit");
            Console.WriteLine();
            Console.Write("Enter choice: ");

            var choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    SendCannedMessage();
                    break;
                case "2":
                    ListCharacteristics();
                    break;
                case "3":
                    ViewLogs();
                    break;
                case "4":
                    _isRunning = false;
                    Console.WriteLine("Shutting down...");
                    break;
                default:
                    Console.WriteLine("Invalid choice. Please try again.");
                    break;
            }
        }

        await Task.CompletedTask;
    }

    private static void SendCannedMessage()
    {
        if (_settings == null || _settings.Bluetooth.CannedMessages.Count == 0)
        {
            Console.WriteLine("No canned messages configured.");
            return;
        }

        Console.WriteLine();
        Console.WriteLine("Available Canned Messages:");
        for (int i = 0; i < _settings.Bluetooth.CannedMessages.Count; i++)
        {
            var msg = _settings.Bluetooth.CannedMessages[i];
            Console.WriteLine($"{i + 1}. {msg.Name} (to {msg.CharacteristicUuid})");
        }
        Console.WriteLine();
        Console.Write("Enter message number: ");

        if (int.TryParse(Console.ReadLine(), out int choice) && choice > 0 && choice <= _settings.Bluetooth.CannedMessages.Count)
        {
            var message = _settings.Bluetooth.CannedMessages[choice - 1];
            
            if (_characteristicsByUuid.TryGetValue(message.CharacteristicUuid, out var characteristic))
            {
                try
                {
                    var data = Convert.FromHexString(message.Data);
                    characteristic.Value = data;
                    
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"✓ Sent '{message.Name}': {message.Data}");
                    Console.ResetColor();
                    
                    if (_logger != null)
                    {
                        _logger.Info($"[NOTIFY] Sent canned message '{message.Name}' to {message.CharacteristicUuid}: {message.Data}");
                    }
                    
                    // In a real implementation, this would trigger a notification to connected clients
                    Console.WriteLine("  (Note: In a full implementation, this would notify connected BLE clients)");
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"✗ Failed to send message: {ex.Message}");
                    Console.ResetColor();
                    
                    if (_logger != null)
                    {
                        _logger.Error($"Failed to send canned message: {ex.Message}");
                    }
                }
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"⚠ Characteristic {message.CharacteristicUuid} not found");
                Console.ResetColor();
            }
        }
        else
        {
            Console.WriteLine("Invalid choice.");
        }
    }

    private static void ListCharacteristics()
    {
        Console.WriteLine();
        Console.WriteLine("Registered Characteristics:");
        Console.WriteLine("─────────────────────────────────────");
        
        if (_application == null || _application.Services.Count == 0)
        {
            Console.WriteLine("No characteristics registered.");
            return;
        }

        foreach (var service in _application.Services)
        {
            Console.WriteLine($"Service: {service.UUID}");
            foreach (var characteristic in service.Characteristics)
            {
                Console.WriteLine($"  └─ {characteristic.UUID}");
                Console.WriteLine($"     Flags: {string.Join(", ", characteristic.Flags)}");
                Console.WriteLine($"     Value: {BitConverter.ToString(characteristic.Value).Replace("-", "")}");
            }
        }
    }

    private static void ViewLogs()
    {
        if (_logger == null)
        {
            Console.WriteLine("Logger not initialized.");
            return;
        }

        Console.WriteLine();
        Console.WriteLine($"Logs are being written to: {_settings!.Logging.LogDirectory}");
        Console.WriteLine("View them with: cat logs/btsimulator-*.log");
        Console.WriteLine("Or tail -f logs/btsimulator-*.log for live updates");
    }

    private static void OnDeviceConnected(object? sender, DeviceConnectionEventArgs e)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine();
        Console.WriteLine("╔════════════════════════════════════════╗");
        Console.WriteLine("║     DEVICE CONNECTED                   ║");
        Console.WriteLine("╚════════════════════════════════════════╝");
        Console.WriteLine($"Device Address: {e.DeviceAddress}");
        Console.WriteLine($"Timestamp: {e.Timestamp:yyyy-MM-dd HH:mm:ss}");
        Console.ResetColor();
        
        if (_logger != null)
        {
            _logger.Info($"[CONNECTION] Device connected: {e.DeviceAddress} at {e.Timestamp}");
        }

        // Send connection message if configured
        if (_settings?.Bluetooth.ConnectionMessage != null)
        {
            SendConnectionMessage();
        }
    }

    private static void OnDeviceDisconnected(object? sender, DeviceConnectionEventArgs e)
    {
        Console.ForegroundColor = ConsoleColor.DarkYellow;
        Console.WriteLine();
        Console.WriteLine("╔════════════════════════════════════════╗");
        Console.WriteLine("║     DEVICE DISCONNECTED                ║");
        Console.WriteLine("╚════════════════════════════════════════╝");
        Console.WriteLine($"Device Address: {e.DeviceAddress}");
        Console.WriteLine($"Timestamp: {e.Timestamp:yyyy-MM-dd HH:mm:ss}");
        Console.ResetColor();
        
        if (_logger != null)
        {
            _logger.Info($"[DISCONNECTION] Device disconnected: {e.DeviceAddress} at {e.Timestamp}");
        }
    }

    private static void SendConnectionMessage()
    {
        if (_settings?.Bluetooth.ConnectionMessage == null)
        {
            return;
        }

        var connMsg = _settings.Bluetooth.ConnectionMessage;
        
        if (_characteristicsByUuid.TryGetValue(connMsg.CharacteristicUuid, out var characteristic))
        {
            try
            {
                var data = Convert.FromHexString(connMsg.Data);
                characteristic.Value = data;
                
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"✓ Sent connection message: {connMsg.Data}");
                Console.WriteLine($"  Description: {connMsg.Description}");
                Console.ResetColor();
                
                if (_logger != null)
                {
                    _logger.Info($"[CONNECTION_MSG] Sent: {connMsg.Data} ({connMsg.Description})");
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"✗ Failed to send connection message: {ex.Message}");
                Console.ResetColor();
                
                if (_logger != null)
                {
                    _logger.Error($"Failed to send connection message: {ex.Message}");
                }
            }
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"⚠ Connection message characteristic {connMsg.CharacteristicUuid} not found");
            Console.ResetColor();
            
            if (_logger != null)
            {
                _logger.Warning($"Connection message characteristic {connMsg.CharacteristicUuid} not found");
            }
        }
    }
}
