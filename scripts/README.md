# BTSimulator Testing Scripts

This directory contains helper scripts for testing and validating Bluetooth LE devices.

## Scripts Overview

### 1. verify-environment.sh
**Purpose**: Verifies that the system is properly configured for BTSimulator.

**Usage**:
```bash
./verify-environment.sh
```

**Checks**:
- Linux/WSL2 platform detection
- BlueZ installation (bluetoothd, bluetoothctl, hciconfig)
- D-Bus system bus connectivity
- User permissions (bluetooth group membership)
- Bluetooth adapter presence

---

### 2. scan-ble-devices.sh
**Purpose**: Scans for nearby Bluetooth LE devices and displays their properties.

**Usage**:
```bash
./scan-ble-devices.sh [duration_seconds]
```

**Examples**:
```bash
# Scan for 10 seconds (default)
./scan-ble-devices.sh

# Scan for 30 seconds
./scan-ble-devices.sh 30
```

**Output**: Lists discovered devices with MAC addresses, names, RSSI, and UUIDs.

---

### 3. test-ble-connection.sh
**Purpose**: Tests connecting to a BLE device and reading its GATT services.

**Usage**:
```bash
./test-ble-connection.sh <device_mac_address>
```

**Examples**:
```bash
./test-ble-connection.sh AA:BB:CC:DD:EE:FF
```

**Features**:
- Connects to the specified device
- Reads device information
- Discovers GATT services and characteristics
- Lists attributes
- Disconnects cleanly

**Requirements**: `bluetoothctl`, optionally `gatttool` for detailed service info

---

### 4. test-read-write.sh
**Purpose**: Tests reading and writing to GATT characteristics.

**Usage**:
```bash
# Read a characteristic
./test-read-write.sh <device_mac> <characteristic_uuid>

# Write and read back
./test-read-write.sh <device_mac> <characteristic_uuid> <hex_value>
```

**Examples**:
```bash
# Read device name characteristic (use handle, not UUID)
./test-read-write.sh AA:BB:CC:DD:EE:FF 0x0010

# Write to a characteristic (value in hex, no 0x prefix)
./test-read-write.sh AA:BB:CC:DD:EE:FF 0x0010 48656c6c6f
```

**Note**: This script requires handle values (e.g., `0x0010`), not UUIDs. Use `gatttool -b <MAC> --characteristics` to find handle values.

**Requirements**: `gatttool` (install with `sudo apt-get install bluez-deprecated`)

---

### 5. test-notifications.sh
**Purpose**: Tests GATT characteristic notifications.

**Usage**:
```bash
./test-notifications.sh <device_mac> <characteristic_handle> [duration_seconds]
```

**Examples**:
```bash
# Listen for 30 seconds (default)
./test-notifications.sh AA:BB:CC:DD:EE:FF 0x0010

# Listen for 60 seconds
./test-notifications.sh AA:BB:CC:DD:EE:FF 0x0010 60
```

**Requirements**: 
- `gatttool` (install with `sudo apt-get install bluez-deprecated`)
- `expect` (install with `sudo apt-get install expect`)

---

## Testing Your BTSimulator Device

### Quick Start Test Flow

1. **Verify Environment**:
   ```bash
   ./verify-environment.sh
   ```

2. **Start Your BTSimulator Device** (in another terminal):
   ```bash
   cd ../src/BTSimulator.Demo
   dotnet run
   ```

3. **Scan for Your Device**:
   ```bash
   ./scan-ble-devices.sh 15
   ```
   Look for your device name (default: "BT Simulator") and note its MAC address.

4. **Connect and Explore**:
   ```bash
   ./test-ble-connection.sh <YOUR_DEVICE_MAC>
   ```

5. **Test Read/Write Operations**:
   ```bash
   # Replace UUID with your characteristic's UUID
   ./test-read-write.sh <YOUR_DEVICE_MAC> 0x0010
   ```

6. **Test Notifications** (if your characteristics support notify):
   ```bash
   # Replace handle with your characteristic's handle
   ./test-notifications.sh <YOUR_DEVICE_MAC> 0x0010 30
   ```

---

## Troubleshooting

### Permission Issues
If you get permission errors:
```bash
# Add yourself to the bluetooth group
sudo usermod -a -G bluetooth $USER

# Log out and log back in, or use:
newgrp bluetooth
```

### BlueZ Not Running
```bash
# Check BlueZ status
sudo systemctl status bluetooth

# Start BlueZ if needed
sudo systemctl start bluetooth
```

### Adapter Not Found
```bash
# List adapters
hciconfig

# Power on adapter
sudo hciconfig hci0 up
```

### WSL2 Specific Issues
- Ensure USB passthrough is configured for your Bluetooth adapter
- Check Windows device manager to verify adapter is passed through
- See `docs/linux-setup.md` for detailed WSL2 setup instructions

---

## Advanced Usage

### Automated Testing
You can combine these scripts for automated testing:

```bash
#!/bin/bash
# automated-test.sh

# Verify environment
./verify-environment.sh || exit 1

# Start simulator in background
cd ../src/BTSimulator.Demo
dotnet run &
SIM_PID=$!
cd ../../scripts

# Wait for startup
sleep 5

# Scan for device
./scan-ble-devices.sh 10 > scan_results.txt

# Extract device MAC (assuming device name is "BT Simulator")
DEVICE_MAC=$(grep -A1 "BT Simulator" scan_results.txt | grep "MAC" | awk '{print $3}')

if [ -n "$DEVICE_MAC" ]; then
    echo "Found device: $DEVICE_MAC"
    
    # Run connection test
    ./test-ble-connection.sh "$DEVICE_MAC"
    
    # Run read/write tests
    # ./test-read-write.sh "$DEVICE_MAC" 0x0010
else
    echo "Device not found!"
fi

# Cleanup
kill $SIM_PID 2>/dev/null
```

---

## Platform Support

These scripts are designed for:
- **Linux**: Full support
- **WSL2**: Supported with USB passthrough
- **Windows/macOS**: Not supported (use Linux VM or WSL2)

---

## Contributing

To add new test scripts:
1. Follow the naming convention: `test-<feature>.sh`
2. Include usage instructions in the script header
3. Make the script executable: `chmod +x script-name.sh`
4. Update this README with script documentation
5. Test on both native Linux and WSL2 if possible

---

## References

- [BlueZ Documentation](http://www.bluez.org/)
- [GATT Specification](https://www.bluetooth.com/specifications/gatt/)
- [Bluetooth Core Specification](https://www.bluetooth.com/specifications/bluetooth-core-specification/)
