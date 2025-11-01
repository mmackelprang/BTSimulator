# Troubleshooting Guide

Common issues and solutions for BTSimulator on Linux and WSL2.

## Table of Contents

1. [Environment Issues](#environment-issues)
2. [BlueZ Issues](#bluez-issues)
3. [D-Bus Issues](#d-bus-issues)
4. [WSL2-Specific Issues](#wsl2-specific-issues)
5. [Permission Issues](#permission-issues)
6. [Adapter Selection Issues](#adapter-selection-issues)
7. [Build and Runtime Issues](#build-and-runtime-issues)

---

## Environment Issues

### Issue: Verification Script Fails

**Symptoms**:
```bash
./scripts/verify-environment.sh
# Shows multiple failures
```

**Solution**:
1. Check which specific tests failed
2. Follow the setup guide for failed components: [linux-setup.md](linux-setup.md)
3. Run verification again after fixes

### Issue: .NET SDK Not Found

**Symptoms**:
```bash
dotnet: command not found
```

**Solution**:
```bash
# Install .NET SDK
wget https://dot.net/v1/dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh --channel 9.0

# Add to PATH (add to ~/.bashrc for persistence)
export DOTNET_ROOT=$HOME/.dotnet
export PATH=$PATH:$DOTNET_ROOT
```

---

## BlueZ Issues

### Issue: BlueZ Service Not Running

**Symptoms**:
```bash
sudo systemctl status bluetooth
# Shows: inactive (dead)
```

**Solution**:
```bash
# Start BlueZ service
sudo systemctl start bluetooth

# Enable auto-start on boot
sudo systemctl enable bluetooth

# Check logs for errors
sudo journalctl -u bluetooth -n 50
```

### Issue: No Bluetooth Adapter Found

**Symptoms**:
```bash
hciconfig
# No output or "Can't get device list"
```

**Solutions**:

**For Native Linux**:
1. Check if adapter is blocked:
```bash
rfkill list bluetooth
# If blocked:
sudo rfkill unblock bluetooth
```

2. Check USB connection:
```bash
lsusb | grep -i bluetooth
dmesg | grep -i bluetooth
```

3. Install firmware (if missing):
```bash
sudo apt-get install linux-firmware
```

**For WSL2**:
- See [WSL2-Specific Issues](#wsl2-specific-issues)

### Issue: Adapter Powers Off Immediately

**Symptoms**:
```bash
sudo hciconfig hci0 up
# Immediately goes back down
```

**Solution**:
```bash
# Check for conflicting services
systemctl list-units | grep -i bluetooth

# Disable conflicting services
sudo systemctl stop bluetooth.target
sudo systemctl start bluetooth

# Check kernel logs
dmesg | tail -50
```

### Issue: Peripheral Mode Not Supported

**Symptoms**:
```
Error: Peripheral mode not available
```

**Solution**:
1. Enable experimental features in BlueZ:
```bash
sudo nano /etc/bluetooth/main.conf
```

Add/uncomment:
```ini
[General]
Experimental = true
```

2. Restart BlueZ:
```bash
sudo systemctl restart bluetooth
```

3. Check if adapter supports LE advertising:
```bash
sudo btmgmt info
# Look for "le" and "advertising" in supported features
```

---

## D-Bus Issues

### Issue: D-Bus Connection Failed

**Symptoms**:
```
Error: Failed to connect to D-Bus
```

**Solution**:
```bash
# Check D-Bus service
sudo systemctl status dbus

# Start D-Bus if not running
sudo systemctl start dbus

# Verify D-Bus socket exists
ls -la /var/run/dbus/system_bus_socket

# Test D-Bus connection
dbus-send --system --print-reply --dest=org.freedesktop.DBus /org/freedesktop/DBus org.freedesktop.DBus.ListNames
```

### Issue: Permission Denied on D-Bus

**Symptoms**:
```
org.freedesktop.DBus.Error.AccessDenied: Permission denied
```

**Solution**:
1. Check user groups:
```bash
groups
# Should include 'bluetooth'
```

2. Add to bluetooth group:
```bash
sudo usermod -aG bluetooth $USER
# Log out and back in
```

3. Check D-Bus policy:
```bash
ls /etc/dbus-1/system.d/bluetooth.conf
# Should exist with proper permissions
```

4. Reload D-Bus:
```bash
sudo systemctl reload dbus
```

### Issue: BlueZ Service Not Found on D-Bus

**Symptoms**:
```
org.freedesktop.DBus.Error.ServiceUnknown: The name org.bluez was not provided
```

**Solution**:
```bash
# Check if BlueZ is running
sudo systemctl status bluetooth

# List D-Bus services
dbus-send --system --print-reply --dest=org.freedesktop.DBus / org.freedesktop.DBus.ListNames | grep bluez

# Restart BlueZ
sudo systemctl restart bluetooth

# Check BlueZ registration
sudo dbus-send --system --print-reply --dest=org.bluez / org.freedesktop.DBus.Introspectable.Introspect
```

---

## WSL2-Specific Issues

### Issue: USB Bluetooth Adapter Not Visible

**Symptoms**:
```bash
lsusb
# Bluetooth adapter not listed
```

**Solutions**:

**For Windows 11**:
```powershell
# In PowerShell as Administrator

# List USB devices
usbipd list

# Bind Bluetooth adapter (replace X-X with bus ID)
usbipd bind --busid X-X

# Attach to WSL
usbipd attach --wsl --busid X-X

# Verify in WSL
wsl lsusb
```

**For Windows 10**:
1. Install usbipd-win from: https://github.com/dorssel/usbipd-win/releases

2. In WSL, install tools:
```bash
sudo apt install linux-tools-generic hwdata
sudo update-alternatives --install /usr/local/bin/usbip usbip /usr/lib/linux-tools/*-generic/usbip 20
```

3. Follow Windows 11 steps above

### Issue: USB Device Disconnects in WSL2

**Symptoms**:
- Adapter works initially but disconnects
- `lsusb` shows adapter intermittently

**Solutions**:
1. Disable USB selective suspend in Windows:
   - Device Manager → Bluetooth Adapter → Properties → Power Management
   - Uncheck "Allow computer to turn off this device"

2. Keep WSL session active:
```bash
# Run in background to keep USB connection
while true; do sleep 60; done &
```

3. Use persistent attachment script (PowerShell):
```powershell
# Run on Windows startup
while ($true) {
    $connected = usbipd list | Select-String "BUSID" -Context 0,20 | Select-String "Attached"
    if (-not $connected) {
        usbipd attach --wsl --busid X-X
    }
    Start-Sleep -Seconds 30
}
```

### Issue: WSL2 Kernel Too Old

**Symptoms**:
```
Error: Kernel version too old for Bluetooth support
```

**Solution**:
```bash
# Check kernel version
uname -r

# Update WSL2
# In PowerShell:
wsl --update

# Update WSL kernel
wsl --shutdown
# WSL will restart with new kernel on next use
```

### Issue: Can't Access /sys/class/bluetooth

**Symptoms**:
```bash
ls /sys/class/bluetooth
# Permission denied or doesn't exist
```

**Solution**:
This is a WSL2 limitation. Use D-Bus interface instead:
```bash
# Use bluetoothctl instead of direct sysfs access
bluetoothctl list
```

---

## Permission Issues

### Issue: Operation Not Permitted

**Symptoms**:
```
Error: Operation not permitted
```

**Solutions**:

1. **Run as root** (temporary):
```bash
sudo dotnet run
```

2. **Add to bluetooth group** (recommended):
```bash
sudo usermod -aG bluetooth $USER
newgrp bluetooth  # Or log out and back in
```

3. **Set capabilities** (advanced):
```bash
# Grant network admin capability to dotnet
sudo setcap 'cap_net_admin,cap_net_raw+eip' /usr/share/dotnet/dotnet

# Verify
getcap /usr/share/dotnet/dotnet
```

**Warning**: Setting capabilities grants significant permissions. Use with caution.

### Issue: Can't Modify MAC Address

**Symptoms**:
```
Error: Cannot set device address
```

**Solution**:
MAC address modification requires:
1. Root privileges OR CAP_NET_ADMIN capability
2. Adapter support (not all adapters allow MAC spoofing)

```bash
# Try with root
sudo ./your-application

# Check if adapter supports address change
sudo btmgmt info
```

Note: Some Bluetooth adapters have hard-coded MAC addresses that cannot be changed.

---

## Adapter Selection Issues

### Issue: Wrong Adapter Selected

**Symptoms**:
- Application uses built-in Bluetooth instead of USB dongle
- Unexpected adapter being used

**Solutions**:

1. **Check available adapters**:
```bash
bluetoothctl list
# Or
hciconfig -a
```

2. **Configure adapter in appsettings.json**:
```json
{
  "Bluetooth": {
    "AdapterName": "hci1"
  }
}
```

3. **Specify adapter on command line (Scanner)**:
```bash
dotnet run --project src/BTSimulator.Scanner/BTSimulator.Scanner.csproj -- 10 json hci1
```

4. **Use interactive selection**:
- Remove `AdapterName` from configuration
- Application will prompt for selection when multiple adapters exist

### Issue: Configured Adapter Not Found

**Symptoms**:
```
Warning: Configured adapter 'hci1' not found
Using default adapter: hci0
```

**Solutions**:

1. **Verify adapter exists**:
```bash
bluetoothctl list
```

2. **Check adapter name/path format**:
   - Use short name: `"hci0"` or `"hci1"`
   - Or full path: `"/org/bluez/hci0"`

3. **Check adapter is powered**:
```bash
bluetoothctl show hci1
# If powered off:
bluetoothctl power on
```

4. **Update configuration** with correct adapter name

### Issue: No Adapters Found

**Symptoms**:
```
Error: No Bluetooth adapters found
```

**Solutions**:

See [No Bluetooth Adapter Found](#issue-no-bluetooth-adapter-found) section above.

### Issue: Can't List Multiple Adapters

**Symptoms**:
- Only one adapter shown even when multiple exist
- Adapter selection not appearing

**Solutions**:

1. **Check BlueZ service**:
```bash
sudo systemctl status bluetooth
```

2. **Verify D-Bus permissions**:
```bash
# Should succeed without error
dbus-send --system --print-reply --dest=org.bluez / org.freedesktop.DBus.ObjectManager.GetManagedObjects
```

3. **Check all adapters are up**:
```bash
sudo hciconfig -a
sudo rfkill list bluetooth
```

4. **Restart BlueZ** to refresh adapter list:
```bash
sudo systemctl restart bluetooth
```

---

## Build and Runtime Issues

### Issue: Build Fails with Missing Assembly

**Symptoms**:
```
error CS0246: The type or namespace name 'Tmds' could not be found
```

**Solution**:
```bash
# Restore NuGet packages
dotnet restore

# Clean and rebuild
dotnet clean
dotnet build
```

### Issue: Tests Fail on Non-Linux Platform

**Symptoms**:
```
Tests fail when run on Windows or macOS
```

**Solution**:
Tests are designed for Linux/WSL2. Expected behavior:
- `IsLinuxEnvironment()` returns `false` on non-Linux
- BlueZ-related tests will fail gracefully

Run tests in WSL2:
```bash
wsl
cd /path/to/BTSimulator
dotnet test
```

### Issue: NotImplementedException in BlueZ Operations

**Symptoms**:
```csharp
await adapter.GetAddressAsync();
// Throws: NotImplementedException: D-Bus property access will be implemented in Phase 2
```

**Solution**:
This is expected in the current phase. Full D-Bus implementation is in progress.

Current status:
- ✅ Environment verification
- ✅ Device configuration
- ✅ D-Bus connection framework
- 🚧 Property access (in progress)
- ⏳ Advertisement registration (planned)
- ⏳ GATT services (planned)

### Issue: Application Crashes on Startup

**Symptoms**:
```
Segmentation fault
```

**Solutions**:

1. Check dependencies:
```bash
ldd /usr/share/dotnet/dotnet
# Verify all libraries load
```

2. Update .NET runtime:
```bash
dotnet --info
# Ensure latest version

sudo apt-get update
sudo apt-get install --only-upgrade dotnet-sdk-9.0
```

3. Check system logs:
```bash
journalctl -xe
dmesg | tail -50
```

---

## Performance Issues

### Issue: Slow Advertisement Response

**Symptoms**:
- Slow to appear in BLE scans
- Intermittent visibility

**Solutions**:

1. Check advertising interval:
```bash
sudo btmgmt adv-info
```

2. Reduce advertising interval (faster, more power):
```bash
# Set min/max interval (units of 0.625ms)
# Example: 100ms = 160 units
sudo btmgmt add-adv -u 180F -i 160 -I 160 1
```

3. Check system load:
```bash
top
# High CPU usage can delay responses
```

### Issue: Connection Drops Frequently

**Symptoms**:
- Clients disconnect unexpectedly
- Unstable connections

**Solutions**:

1. Check signal strength (if USB adapter):
```bash
# Use adapter with external antenna
# Keep adapter away from interference sources
```

2. Increase connection interval:
```bash
sudo btmgmt conn-info
```

3. Check system resources:
```bash
free -h
# Ensure adequate memory

ps aux | grep bluetooth
# Check for multiple Bluetooth processes
```

---

## Getting Help

### Collect Diagnostic Information

When reporting issues, include:

1. **Environment info**:
```bash
./scripts/verify-environment.sh > diagnostics.txt
uname -a >> diagnostics.txt
dotnet --info >> diagnostics.txt
```

2. **BlueZ info**:
```bash
bluetoothctl --version >> diagnostics.txt
sudo systemctl status bluetooth >> diagnostics.txt
sudo btmgmt info >> diagnostics.txt
```

3. **D-Bus info**:
```bash
dbus-send --system --print-reply --dest=org.freedesktop.DBus / org.freedesktop.DBus.ListNames >> diagnostics.txt
```

4. **Logs**:
```bash
sudo journalctl -u bluetooth -n 100 > bluetooth.log
dmesg | grep -i bluetooth > kernel.log
```

### Resources

- **GitHub Issues**: https://github.com/mmackelprang/BTSimulator/issues
- **BlueZ Mailing List**: linux-bluetooth@vger.kernel.org
- **WSL GitHub**: https://github.com/microsoft/WSL/issues
- **Stack Overflow**: Tag with `bluez`, `dbus`, or `wsl2`

### Community

- Report bugs on GitHub with diagnostic information
- Check existing issues for similar problems
- Provide feedback on documentation clarity

---

## Known Limitations

### Current Implementation Limits

1. **MAC Address Spoofing**:
   - Not fully implemented
   - Requires special permissions
   - Not supported by all adapters

2. **Classic Bluetooth**:
   - Only BLE (Bluetooth Low Energy) currently supported
   - Classic profiles (A2DP, HID) are stretch goals

3. **Multiple Adapters**:
   - Supports multiple adapters with selection
   - Interactive or configuration-based selection available
   - See [Adapter Selection](#adapter-selection-issues) below

### Platform Limitations

1. **WSL2**:
   - USB passthrough required
   - Some latency vs native Linux
   - Limited low-level hardware access

2. **BlueZ**:
   - Requires version 5.x+
   - Some features require experimental mode
   - Peripheral mode support varies by adapter

3. **D-Bus**:
   - System bus permissions required
   - Complex message protocol
   - Limited error details in some cases

---

## Advanced Troubleshooting

### Enable BlueZ Debug Logging

```bash
# Edit systemd service
sudo systemctl edit bluetooth.service
```

Add:
```ini
[Service]
ExecStart=
ExecStart=/usr/lib/bluetooth/bluetoothd -d -n
```

Restart and check logs:
```bash
sudo systemctl restart bluetooth
sudo journalctl -u bluetooth -f
```

### Monitor D-Bus Traffic

```bash
# Install dbus-monitor
sudo apt-get install dbus

# Monitor all system bus traffic
sudo dbus-monitor --system

# Filter for BlueZ
sudo dbus-monitor --system "type='signal',sender='org.bluez'"
```

### Use btmon for Bluetooth Traffic

```bash
# Install bluez tools
sudo apt-get install bluez-hcidump

# Monitor Bluetooth HCI traffic
sudo btmon

# Save to file
sudo btmon -w capture.log
```

### Test with bluetoothctl

```bash
# Interactive Bluetooth shell
sudo bluetoothctl

# Commands:
list                 # List adapters
show [ctrl]          # Show adapter info
power on             # Power on adapter
advertise on         # Start advertising
advertise.name "Test" # Set advertise name
```
