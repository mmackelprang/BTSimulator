# Linux and WSL2 Setup Guide

This guide provides detailed instructions for setting up your Linux or WSL2 environment to run BTSimulator.

## Table of Contents

1. [Linux Setup](#linux-setup)
2. [WSL2 Setup](#wsl2-setup)
3. [BlueZ Installation](#bluez-installation)
4. [D-Bus Configuration](#d-bus-configuration)
5. [USB Bluetooth Adapter (WSL2)](#usb-bluetooth-adapter-wsl2)
6. [Permissions Configuration](#permissions-configuration)
7. [Verification](#verification)

## Linux Setup

### Prerequisites

- Ubuntu 20.04 LTS or later (or equivalent Debian-based distribution)
- Sudo privileges
- Internet connection

### Install .NET SDK

```bash
# Add Microsoft package repository
wget https://packages.microsoft.com/config/ubuntu/$(lsb_release -rs)/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb

# Install .NET SDK 9.0
sudo apt-get update
sudo apt-get install -y dotnet-sdk-9.0
```

Verify installation:
```bash
dotnet --version
```

## WSL2 Setup

### Enable WSL2 on Windows

#### Windows 11 (Recommended)

Windows 11 has built-in USB passthrough support:

```powershell
# Run in PowerShell as Administrator
wsl --install -d Ubuntu-22.04
```

#### Windows 10

For Windows 10, you'll need usbipd-win:

1. Install WSL2:
```powershell
# Run in PowerShell as Administrator
wsl --install
wsl --set-default-version 2
```

2. Install usbipd-win from: https://github.com/dorssel/usbipd-win/releases

### Install Linux Distribution

```powershell
# List available distributions
wsl --list --online

# Install Ubuntu (recommended)
wsl --install -d Ubuntu-22.04
```

### Update WSL2 Kernel

```bash
# Inside WSL2
sudo apt update && sudo apt upgrade -y
```

## BlueZ Installation

BlueZ is the official Linux Bluetooth stack.

### Install BlueZ

```bash
# Update package lists
sudo apt-get update

# Install BlueZ and tools
sudo apt-get install -y bluez bluez-tools

# Verify installation
bluetoothctl --version
```

### Start BlueZ Service

```bash
# Enable and start Bluetooth service
sudo systemctl enable bluetooth
sudo systemctl start bluetooth

# Check status
sudo systemctl status bluetooth
```

### BlueZ Configuration

Edit `/etc/bluetooth/main.conf` for peripheral mode:

```bash
sudo nano /etc/bluetooth/main.conf
```

Ensure these settings are uncommented and set:

```ini
[General]
# Enable experimental features (required for peripheral mode)
Experimental = true

[Policy]
# Allow auto-enable of controllers
AutoEnable = true
```

Restart BlueZ after changes:

```bash
sudo systemctl restart bluetooth
```

## D-Bus Configuration

D-Bus is required for communication with BlueZ.

### Install D-Bus

```bash
sudo apt-get install -y dbus
```

### Start D-Bus Service

```bash
# Enable and start D-Bus
sudo systemctl enable dbus
sudo systemctl start dbus

# Verify D-Bus is running
ps aux | grep dbus-daemon
```

### D-Bus Permissions

Create a D-Bus policy file for BlueZ access:

```bash
sudo nano /etc/dbus-1/system.d/bluetooth.conf
```

Add the following (if not present):

```xml
<!DOCTYPE busconfig PUBLIC
 "-//freedesktop//DTD D-BUS Bus Configuration 1.0//EN"
 "http://www.freedesktop.org/standards/dbus/1.0/busconfig.dtd">
<busconfig>
  <policy user="root">
    <allow own="org.bluez"/>
    <allow send_destination="org.bluez"/>
    <allow send_interface="org.bluez.Agent1"/>
    <allow send_interface="org.bluez.LEAdvertisingManager1"/>
    <allow send_interface="org.bluez.GattManager1"/>
  </policy>
  
  <policy group="bluetooth">
    <allow send_destination="org.bluez"/>
    <allow send_interface="org.bluez.Agent1"/>
    <allow send_interface="org.bluez.LEAdvertisingManager1"/>
    <allow send_interface="org.bluez.GattManager1"/>
  </policy>
</busconfig>
```

Reload D-Bus configuration:

```bash
sudo systemctl reload dbus
```

## USB Bluetooth Adapter (WSL2)

### Windows 11 - Native USB Passthrough

1. List USB devices in PowerShell (as Administrator):

```powershell
usbipd list
```

2. Attach Bluetooth adapter:

```powershell
# Replace X-X with your device's bus ID
usbipd bind --busid X-X
usbipd attach --wsl --busid X-X
```

3. Verify in WSL2:

```bash
lsusb
# Should show your Bluetooth adapter
```

4. Make persistent (optional):

Create a script `attach-bluetooth.ps1`:

```powershell
$busId = "X-X"  # Replace with your bus ID
usbipd attach --wsl --busid $busId
```

### Windows 10 - usbipd-win

1. Install usbipd-win from: https://github.com/dorssel/usbipd-win/releases

2. In WSL2, install USB tools:

```bash
sudo apt install linux-tools-generic hwdata
sudo update-alternatives --install /usr/local/bin/usbip usbip /usr/lib/linux-tools/*-generic/usbip 20
```

3. Follow Windows 11 instructions above for attaching device

### Verify Bluetooth Adapter

```bash
# Check if adapter is visible
hciconfig

# Expected output:
# hci0:   Type: Primary  Bus: USB
#         BD Address: XX:XX:XX:XX:XX:XX  ACL MTU: 1021:8  SCO MTU: 64:1
#         ...

# Check with bluetoothctl
bluetoothctl list
```

## Permissions Configuration

### Add User to Bluetooth Group

```bash
# Add current user to bluetooth group
sudo usermod -aG bluetooth $USER

# Also add to netdev group (helpful for network operations)
sudo usermod -aG netdev $USER

# Log out and log back in for changes to take effect
# Or use: newgrp bluetooth
```

Verify group membership:

```bash
groups
# Should include 'bluetooth' in the output
```

### Alternative: Run as Root

For testing purposes, you can run as root:

```bash
sudo dotnet run --project src/BTSimulator.Core
```

**Warning**: Running as root is not recommended for production use.

### Set Capabilities (Advanced)

For specific executables that need Bluetooth access without root:

```bash
# Give CAP_NET_ADMIN capability to dotnet (use with caution)
sudo setcap 'cap_net_admin+eip' /usr/share/dotnet/dotnet
```

**Note**: This grants significant privileges and should be done carefully.

## Verification

### Run Verification Script

```bash
cd BTSimulator
./scripts/verify-environment.sh
```

The script checks:
- ✓ Linux/WSL2 platform
- ✓ .NET SDK installation
- ✓ BlueZ installation and version
- ✓ D-Bus system bus
- ✓ User permissions
- ✓ Bluetooth adapter detection

### Manual Verification Steps

1. **Check Platform**:
```bash
uname -a
cat /proc/version  # Check for WSL2
```

2. **Check BlueZ**:
```bash
bluetoothctl --version
sudo systemctl status bluetooth
```

3. **Check D-Bus**:
```bash
dbus-send --system --dest=org.freedesktop.DBus --type=method_call --print-reply /org/freedesktop/DBus org.freedesktop.DBus.ListNames
```

4. **Check Bluetooth Adapter**:
```bash
hciconfig
rfkill list bluetooth
```

5. **Test Bluetooth**:
```bash
sudo bluetoothctl
# In bluetoothctl:
power on
agent on
default-agent
scan on
# Should see nearby Bluetooth devices
```

## Troubleshooting

### Bluetooth Service Not Starting

```bash
# Check logs
sudo journalctl -u bluetooth -n 50

# Restart service
sudo systemctl restart bluetooth

# Check for blocked adapters
rfkill list
sudo rfkill unblock bluetooth
```

### D-Bus Connection Issues

```bash
# Verify D-Bus socket
ls -la /var/run/dbus/system_bus_socket

# Test D-Bus connection
dbus-send --system --print-reply --dest=org.freedesktop.DBus /org/freedesktop/DBus org.freedesktop.DBus.ListNames
```

### USB Passthrough Not Working (WSL2)

```bash
# Windows side - check device status
usbipd list
usbipd attach --wsl --busid X-X

# WSL2 side - check USB
lsusb
dmesg | grep -i bluetooth
```

### Permission Denied Errors

```bash
# Verify group membership
groups

# Re-login or use newgrp
newgrp bluetooth

# Check D-Bus permissions
sudo dbus-send --system --print-reply --dest=org.bluez /org/bluez org.freedesktop.DBus.Introspectable.Introspect
```

## Additional Resources

- [BlueZ Documentation](http://www.bluez.org/)
- [WSL2 USB Support](https://learn.microsoft.com/en-us/windows/wsl/connect-usb)
- [usbipd-win GitHub](https://github.com/dorssel/usbipd-win)
- [D-Bus Tutorial](https://dbus.freedesktop.org/doc/dbus-tutorial.html)

## Next Steps

After setup is complete:

1. Build the project: `dotnet build`
2. Run tests: `dotnet test`
3. See [README.md](../README.md) for usage examples
4. Check [troubleshooting.md](troubleshooting.md) for common issues
