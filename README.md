# Bluetooth Device Simulator (BTSimulator)

A C# project for WSL2 and Linux that interfaces with BlueZ to simulate Bluetooth LE devices. This project allows runtime configuration of device name, MAC address, and arbitrary GATT service/characteristic UUIDs for Bluetooth peripheral simulation.

## Project Status

### Completed Phases

✅ **Phase 1: Environment Setup**
- C# (.NET 9.0) project structure with core library and tests
- Environment verification for Linux/WSL2, BlueZ, D-Bus, and permissions
- Comprehensive test suite for environment detection
- Linux setup documentation

✅ **Phase 2: D-Bus & BlueZ Integration** (Foundation)
- D-Bus connection framework using Tmds.DBus
- BlueZ API interface definitions and constants
- Manager class for adapter discovery and management
- API documentation with BlueZ references

✅ **Phase 3: Device Configuration** (Foundation)
- Device configuration classes for name and MAC address
- GATT service and characteristic configuration
- Validation framework for device configurations
- Comprehensive unit tests

### In Progress

🚧 **Phase 2-3: Full D-Bus Implementation**
- Complete D-Bus message passing for property access
- Adapter discovery via ObjectManager interface
- Runtime device name and MAC address modification

🚧 **Phase 4: GATT Service Simulation**
- Dynamic GATT service registration
- Characteristic implementation with read/write/notify
- Advertisement broadcasting

🚧 **Phase 5: End-to-End Validation**
- Helper scripts for BLE client testing
- Integration test scenarios
- Edge case validation

## Requirements

### System Requirements
- **Operating System**: Linux or WSL2 (Windows Subsystem for Linux 2)
- **BlueZ**: Version 5.x or later
- **D-Bus**: System bus access required
- **.NET**: .NET 9.0 SDK or later

### WSL2-Specific Requirements
- USB passthrough for Bluetooth adapter (Windows 11 or usbipd-win)
- Bluetooth adapter accessible in WSL2 environment

### Permissions
- User must be in `bluetooth` group OR run as root
- D-Bus system bus access permissions

## Quick Start

### 1. Verify Environment

```bash
# Run environment verification script
./scripts/verify-environment.sh
```

This script checks for:
- Linux/WSL2 platform
- BlueZ installation (bluetoothd, bluetoothctl, hciconfig)
- D-Bus system bus connectivity
- User permissions (bluetooth group membership)

### 2. Build the Project

```bash
# Clone the repository
git clone https://github.com/mmackelprang/BTSimulator.git
cd BTSimulator

# Build the solution
dotnet build

# Run tests
dotnet test
```

### 3. Run Tests

```bash
# Run all tests
dotnet test

# Run with verbose output
dotnet test --verbosity normal

# Run specific test class
dotnet test --filter "FullyQualifiedName~EnvironmentVerifierTests"
```

## Project Structure

```
BTSimulator/
├── src/
│   ├── BTSimulator.Core/          # Core library
│   │   ├── Environment/           # Environment verification
│   │   ├── BlueZ/                 # BlueZ D-Bus integration
│   │   └── Device/                # Device configuration
│   └── BTSimulator.Tests/         # Unit and integration tests
├── scripts/                       # Helper scripts
│   ├── verify-environment.sh      # Environment verification
│   └── setup-linux.sh             # Linux setup automation
├── docs/                          # Documentation
│   ├── linux-setup.md             # Linux/WSL2 setup guide
│   ├── api-mapping.md             # BlueZ API documentation
│   └── troubleshooting.md         # Common issues and solutions
├── BTSimulator.sln                # Solution file
└── README.md                      # This file
```

## Usage Example

```csharp
using BTSimulator.Core.Environment;
using BTSimulator.Core.Device;
using BTSimulator.Core.BlueZ;

// Verify environment
var verifier = new EnvironmentVerifier();
var envResult = await verifier.VerifyEnvironment();

if (!envResult.IsReady)
{
    Console.WriteLine(envResult.GetSummary());
    return;
}

// Configure a simulated device
var config = new DeviceConfiguration
{
    DeviceName = "My BLE Device",
    DeviceAddress = "AA:BB:CC:DD:EE:FF"
};

// Add a battery service
var batteryService = new GattServiceConfiguration
{
    Uuid = "180F", // Battery Service UUID
    IsPrimary = true
};

var batteryLevel = new GattCharacteristicConfiguration
{
    Uuid = "2A19", // Battery Level UUID
    Flags = new List<string> { "read", "notify" },
    InitialValue = new byte[] { 100 } // 100% battery
};

batteryService.AddCharacteristic(batteryLevel);
config.AddService(batteryService);

// Validate configuration
if (!config.Validate(out var errors))
{
    Console.WriteLine("Configuration errors:");
    errors.ForEach(Console.WriteLine);
    return;
}

// Connect to BlueZ (Phase 2 - In Progress)
var manager = new BlueZManager();
await manager.ConnectAsync();
// Additional implementation in progress...
```

## Documentation

- **[Linux Setup Guide](docs/linux-setup.md)**: Detailed instructions for setting up Linux/WSL2, BlueZ, and USB passthrough
- **[API Mapping](docs/api-mapping.md)**: C# wrapper to BlueZ D-Bus API reference
- **[Troubleshooting](docs/troubleshooting.md)**: Common issues and solutions

## Development

### Building

```bash
dotnet build
```

### Testing

```bash
# Run all tests
dotnet test

# Run with coverage (requires coverlet)
dotnet test /p:CollectCoverage=true
```

### Code Style

- Follow C# naming conventions
- Use XML documentation comments for public APIs
- Include references to BlueZ documentation where applicable
- Document Linux/WSL2 limitations and known issues

## Known Limitations

### WSL2 Limitations
- **USB Passthrough Required**: Bluetooth adapters need USB passthrough to be accessible in WSL2
- **Windows 11 Recommended**: Native USB support; Windows 10 requires usbipd-win
- **Performance**: Some latency may occur due to USB virtualization

### BlueZ Limitations
- **Version Requirements**: BlueZ 5.x required for full peripheral mode support
- **MAC Address Spoofing**: Requires elevated privileges and may not work on all adapters
- **Concurrent Operations**: Limited by adapter capabilities and BlueZ configuration

### D-Bus Limitations
- **System Bus Access**: Requires appropriate permissions or group membership
- **Message Complexity**: Full D-Bus protocol implementation is complex
- **Error Handling**: D-Bus errors may vary by system configuration

## Contributing

Contributions are welcome! Please:
1. Fork the repository
2. Create a feature branch
3. Add tests for new functionality
4. Update documentation
5. Submit a pull request

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## References

- [BlueZ Official Documentation](http://www.bluez.org/)
- [BlueZ D-Bus API](https://git.kernel.org/pub/scm/bluetooth/bluez.git/tree/doc)
- [Tmds.DBus Library](https://github.com/tmds/Tmds.DBus)
- [Bluetooth GATT Specifications](https://www.bluetooth.com/specifications/gatt/)

## Authors

- Mark Mackelprang

## Acknowledgments

- BlueZ project for Linux Bluetooth stack
- Tmds.DBus for D-Bus bindings
- .NET team for cross-platform support
