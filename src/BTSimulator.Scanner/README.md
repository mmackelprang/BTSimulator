# BTScanner - Bluetooth Device Discovery Utility

BTScanner is a command-line utility that scans for local Bluetooth devices and outputs their configuration details in a format ready for use in the Demo application's `appsettings.json` file.

## Features

- Discovers all Bluetooth devices in range
- Extracts device name, MAC address, and signal strength (RSSI)
- Reads GATT services and characteristics
- Identifies known Bluetooth services and characteristics by name
- Outputs configuration in JSON or human-readable text format
- Ready-to-use output for `appsettings.json`

## Requirements

- Linux or WSL2 environment
- BlueZ 5.x or later installed
- Bluetooth adapter powered on
- User in `bluetooth` group or root access
- .NET 9.0 SDK

## Usage

### Basic Scan

Scan for 10 seconds and output JSON (default):
```bash
dotnet run --project src/BTSimulator.Scanner/BTSimulator.Scanner.csproj
```

### Custom Scan Duration

Scan for 20 seconds:
```bash
dotnet run --project src/BTSimulator.Scanner/BTSimulator.Scanner.csproj -- 20
```

### Text Output Format

Scan and display human-readable output:
```bash
dotnet run --project src/BTSimulator.Scanner/BTSimulator.Scanner.csproj -- 10 text
```

### Specify Bluetooth Adapter

If you have multiple Bluetooth adapters, you can specify which one to use:
```bash
# By adapter name
dotnet run --project src/BTSimulator.Scanner/BTSimulator.Scanner.csproj -- 10 json hci0

# By full path
dotnet run --project src/BTSimulator.Scanner/BTSimulator.Scanner.csproj -- 10 json /org/bluez/hci1
```

If you don't specify an adapter and multiple adapters are detected, you will be prompted to select one.

### Command Line Arguments

```
dotnet run --project src/BTSimulator.Scanner/BTSimulator.Scanner.csproj -- [duration] [format] [adapter]

Arguments:
  duration  Scan duration in seconds (default: 10)
  format    Output format: "json" or "text" (default: "json")
  adapter   Adapter name like "hci0" or path like "/org/bluez/hci0" (optional)
```

## Output Examples

### JSON Output

The JSON output can be directly copied to `appsettings.json`:

```json
[
  {
    "deviceName": "Fitness Tracker",
    "deviceAddress": "AA:BB:CC:DD:EE:FF",
    "rssi": -52,
    "services": [
      {
        "uuid": "180F",
        "isPrimary": true,
        "characteristics": [
          {
            "uuid": "2A19",
            "flags": ["read", "notify"],
            "initialValue": "64",
            "description": "Battery Level"
          }
        ]
      },
      {
        "uuid": "180A",
        "isPrimary": true,
        "characteristics": [
          {
            "uuid": "2A29",
            "flags": ["read"],
            "initialValue": "46697453747564696F",
            "description": "Manufacturer Name String"
          },
          {
            "uuid": "2A24",
            "flags": ["read"],
            "initialValue": "46542D313030",
            "description": "Model Number String"
          }
        ]
      }
    ]
  }
]
```

### Text Output

Human-readable output with device details:

```
================================================================================
Device Name: Fitness Tracker
Device Address: AA:BB:CC:DD:EE:FF
RSSI: -52 dBm
Service UUIDs:
  - 180F (Battery Service)
  - 180A (Device Information)

GATT Services:
  Service: 180F (Battery Service)
  Primary: True
  Characteristics:
    - UUID: 2A19 (Battery Level)
      Flags: read, notify
      Value: 64

  Service: 180A (Device Information)
  Primary: True
  Characteristics:
    - UUID: 2A29 (Manufacturer Name String)
      Flags: read
      Value: 46697453747564696F
    - UUID: 2A24 (Model Number String)
      Flags: read
      Value: 46542D313030

Configuration for appsettings.json:
```json
{
  "bluetooth": {
    "deviceName": "Fitness Tracker",
    "deviceAddress": "AA:BB:CC:DD:EE:FF",
    "services": [
      {
        "uuid": "180F",
        "isPrimary": true,
        "characteristics": [
          {
            "uuid": "2A19",
            "flags": ["read", "notify"],
            "initialValue": "64",
            "description": "Battery Level"
          }
        ]
      }
    ]
  }
}
```
```

## Using Discovered Devices

1. Run BTScanner to discover devices:
   ```bash
   dotnet run --project src/BTSimulator.Scanner/BTSimulator.Scanner.csproj
   ```

2. Copy the JSON output for the device you want to simulate

3. Paste it into the Demo project's `appsettings.json` file under the `Bluetooth` section:
   ```json
   {
     "Logging": {
       "LogDirectory": "logs",
       "MinLevel": "Debug"
     },
     "Bluetooth": {
       "DeviceName": "Fitness Tracker",
       "DeviceAddress": "AA:BB:CC:DD:EE:FF",
       "Services": [
         // ... services from BTScanner output
       ]
     }
   }
   ```

4. Run the Demo application to simulate the device

## Troubleshooting

### "Failed to connect to BlueZ"

**Cause**: BlueZ daemon is not running or not accessible

**Solutions**:
- Verify BlueZ is installed: `which bluetoothd`
- Check BlueZ service status: `systemctl status bluetooth`
- Start BlueZ service: `sudo systemctl start bluetooth`
- Test with: `bluetoothctl`

### "No Bluetooth adapter found"

**Cause**: No Bluetooth adapter is available or accessible

**Solutions**:
- Check for adapter: `hciconfig` or `bluetoothctl list`
- On WSL2: Ensure USB passthrough is configured
- Verify adapter is not blocked: `rfkill list bluetooth`
- Unblock if needed: `rfkill unblock bluetooth`

### "No devices found"

**Causes**: 
- Devices not in range
- Devices not advertising
- Scan duration too short

**Solutions**:
- Increase scan duration: `dotnet run -- 30`
- Ensure target devices are in pairing/advertising mode
- Move devices closer to adapter
- Verify manual scanning works: `bluetoothctl scan on`

### Permission Errors

**Cause**: User doesn't have Bluetooth permissions

**Solutions**:
- Add user to bluetooth group: `sudo usermod -a -G bluetooth $USER`
- Log out and log back in for group changes to take effect
- Alternative: Run with sudo (not recommended)

### Cannot Read Characteristics

**Cause**: Some characteristics require pairing or authentication

**Notes**:
- BTScanner will show characteristics that are readable without pairing
- Characteristics requiring authentication will show in service list but may not have values
- The scanner will still output the characteristic UUID and flags for configuration

## Known Bluetooth Services

BTScanner recognizes many standard Bluetooth services and characteristics:

**Common Services**:
- 180F: Battery Service
- 180A: Device Information
- 1800: Generic Access
- 180D: Heart Rate
- 1812: Human Interface Device (HID)
- 181C: User Data
- 181D: Weight Scale

**Common Characteristics**:
- 2A19: Battery Level
- 2A29: Manufacturer Name
- 2A24: Model Number
- 2A25: Serial Number
- 2A37: Heart Rate Measurement
- 2A00: Device Name

For a complete list, see the Bluetooth SIG specifications at https://www.bluetooth.com/specifications/assigned-numbers/

## Technical Details

BTScanner uses:
- BlueZ D-Bus API for device discovery
- ObjectManager interface for enumerating devices
- GATT interfaces for reading services and characteristics
- Standard Bluetooth UUID database for naming

The scanner will attempt to connect to devices to read their GATT attributes. Some devices may refuse connections or require pairing. The scanner will gracefully handle these cases and output whatever information is available.
