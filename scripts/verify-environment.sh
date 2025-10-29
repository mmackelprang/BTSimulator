#!/bin/bash

# Bluetooth Device Simulator - Environment Verification Script
# This script checks if the system meets the requirements for running BTSimulator

set -e

# Color codes for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo "======================================"
echo "BTSimulator Environment Verification"
echo "======================================"
echo ""

# Track overall status
ALL_CHECKS_PASSED=true

# Function to print status
print_status() {
    if [ "$1" = "pass" ]; then
        echo -e "${GREEN}✓${NC} $2"
    elif [ "$1" = "fail" ]; then
        echo -e "${RED}✗${NC} $2"
        ALL_CHECKS_PASSED=false
    elif [ "$1" = "warn" ]; then
        echo -e "${YELLOW}⚠${NC} $2"
    else
        echo "  $2"
    fi
}

# 1. Check if running on Linux
echo "1. Platform Check"
if [ "$(uname -s)" = "Linux" ]; then
    print_status "pass" "Running on Linux"
    
    # Check if WSL2
    if grep -qi microsoft /proc/version 2>/dev/null || grep -qi wsl /proc/version 2>/dev/null; then
        print_status "pass" "Running under WSL2"
        print_status "warn" "WSL2 requires USB passthrough for Bluetooth adapter"
    else
        print_status "info" "Running on native Linux"
    fi
else
    print_status "fail" "Not running on Linux (detected: $(uname -s))"
fi
echo ""

# 2. Check .NET installation
echo "2. .NET SDK Check"
if command -v dotnet &> /dev/null; then
    DOTNET_VERSION=$(dotnet --version)
    print_status "pass" ".NET SDK installed (version: $DOTNET_VERSION)"
    
    # Check if .NET 9.0 or later is available
    MAJOR_VERSION=$(echo $DOTNET_VERSION | cut -d. -f1)
    if [ "$MAJOR_VERSION" -ge 9 ]; then
        print_status "pass" ".NET 9.0+ detected"
    else
        print_status "warn" ".NET 9.0 or later recommended (current: $DOTNET_VERSION)"
    fi
else
    print_status "fail" ".NET SDK not found"
    print_status "info" "Install from: https://dotnet.microsoft.com/download"
fi
echo ""

# 3. Check BlueZ installation
echo "3. BlueZ Installation Check"
BLUEZ_FOUND=false

if command -v bluetoothd &> /dev/null; then
    print_status "pass" "bluetoothd daemon found"
    BLUEZ_FOUND=true
else
    print_status "fail" "bluetoothd daemon not found"
fi

if command -v bluetoothctl &> /dev/null; then
    print_status "pass" "bluetoothctl CLI tool found"
    BLUEZ_VERSION=$(bluetoothctl --version 2>&1 | head -n1 || echo "Unknown")
    print_status "info" "Version: $BLUEZ_VERSION"
    BLUEZ_FOUND=true
else
    print_status "fail" "bluetoothctl not found"
fi

if command -v hciconfig &> /dev/null; then
    print_status "pass" "hciconfig tool found"
else
    print_status "warn" "hciconfig not found (legacy tool, not critical)"
fi

if [ "$BLUEZ_FOUND" = false ]; then
    print_status "info" "Install BlueZ: sudo apt-get install bluez"
fi
echo ""

# 4. Check D-Bus installation
echo "4. D-Bus System Bus Check"
if command -v dbus-send &> /dev/null; then
    print_status "pass" "dbus-send tool found"
else
    print_status "fail" "dbus-send not found"
    print_status "info" "Install: sudo apt-get install dbus"
fi

# Check D-Bus socket
if [ -e /var/run/dbus/system_bus_socket ]; then
    print_status "pass" "D-Bus system bus socket found"
else
    print_status "fail" "D-Bus system bus socket not found at /var/run/dbus/system_bus_socket"
fi

# Check if D-Bus service is running
if systemctl is-active --quiet dbus 2>/dev/null || pgrep -x dbus-daemon &> /dev/null; then
    print_status "pass" "D-Bus service is running"
else
    print_status "warn" "D-Bus service may not be running"
fi
echo ""

# 5. Check permissions
echo "5. Permission Check"
USER_GROUPS=$(groups)
IS_ROOT=false
IN_BLUETOOTH_GROUP=false

if [ "$EUID" -eq 0 ]; then
    print_status "pass" "Running as root"
    IS_ROOT=true
elif echo "$USER_GROUPS" | grep -q "\\bbluetooth\\b"; then
    print_status "pass" "User is in 'bluetooth' group"
    IN_BLUETOOTH_GROUP=true
else
    print_status "fail" "User is not in 'bluetooth' group and not running as root"
    print_status "info" "Add user to bluetooth group: sudo usermod -aG bluetooth \$USER"
    print_status "info" "Then log out and log back in"
fi

if echo "$USER_GROUPS" | grep -q "\\bnetdev\\b"; then
    print_status "pass" "User is in 'netdev' group"
fi

if [ "$IS_ROOT" = false ] && [ "$IN_BLUETOOTH_GROUP" = false ]; then
    print_status "warn" "Insufficient permissions for Bluetooth operations"
fi
echo ""

# 6. Check Bluetooth adapter
echo "6. Bluetooth Adapter Check"
if command -v hciconfig &> /dev/null; then
    HCI_OUTPUT=$(hciconfig 2>&1)
    if echo "$HCI_OUTPUT" | grep -q "hci"; then
        print_status "pass" "Bluetooth adapter(s) detected"
        # Show adapter info
        echo "$HCI_OUTPUT" | grep "hci" | while read line; do
            print_status "info" "  $line"
        done
    else
        print_status "fail" "No Bluetooth adapters found"
        if grep -qi microsoft /proc/version 2>/dev/null || grep -qi wsl /proc/version 2>/dev/null; then
            print_status "info" "WSL2: Make sure USB passthrough is configured"
            print_status "info" "See docs/linux-setup.md for instructions"
        fi
    fi
elif command -v bluetoothctl &> /dev/null; then
    # Try bluetoothctl if hciconfig not available
    if timeout 2 bluetoothctl list 2>&1 | grep -q "Controller"; then
        print_status "pass" "Bluetooth adapter(s) detected via bluetoothctl"
    else
        print_status "warn" "Unable to detect Bluetooth adapters"
    fi
else
    print_status "warn" "Cannot check for Bluetooth adapters (tools not available)"
fi
echo ""

# Summary
echo "======================================"
echo "Summary"
echo "======================================"
if [ "$ALL_CHECKS_PASSED" = true ]; then
    print_status "pass" "All required checks passed!"
    echo ""
    echo "You can now build and run BTSimulator:"
    echo "  dotnet build"
    echo "  dotnet test"
    exit 0
else
    print_status "fail" "Some checks failed. Please address the issues above."
    echo ""
    echo "For detailed setup instructions, see: docs/linux-setup.md"
    exit 1
fi
