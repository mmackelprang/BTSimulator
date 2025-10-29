#!/bin/bash
# BLE Device Scanner Script
# Scans for Bluetooth LE devices and displays their properties
# Usage: ./scan-ble-devices.sh [duration_seconds]

set -e

SCAN_DURATION=${1:-10}

echo "=========================================="
echo "BLE Device Scanner"
echo "=========================================="
echo "Scan duration: ${SCAN_DURATION} seconds"
echo ""

# Check if bluetoothctl is available
if ! command -v bluetoothctl &> /dev/null; then
    echo "Error: bluetoothctl not found. Please install bluez."
    exit 1
fi

# Check if running as root or in bluetooth group
if ! groups | grep -q bluetooth && [ "$(id -u)" -ne 0 ]; then
    echo "Warning: Not in bluetooth group. Some operations may fail."
    echo "Run: sudo usermod -a -G bluetooth $USER"
    echo ""
fi

echo "Starting BLE scan..."
echo ""

# Start scanning in background
bluetoothctl << EOF &
power on
scan on
EOF

SCAN_PID=$!

# Wait for scan duration
sleep "$SCAN_DURATION"

# Stop scanning
bluetoothctl << EOF
scan off
EOF

wait $SCAN_PID 2>/dev/null || true

echo ""
echo "Scan complete."
echo ""
echo "Discovered devices:"
echo "==================="

# List devices
bluetoothctl devices | while read -r line; do
    device_mac=$(echo "$line" | awk '{print $2}')
    device_name=$(echo "$line" | cut -d' ' -f3-)
    
    echo ""
    echo "Device: $device_name"
    echo "  MAC Address: $device_mac"
    
    # Get device info
    bluetoothctl info "$device_mac" | grep -E "RSSI|UUID|ManufacturerData" || true
done

echo ""
echo "=========================================="
echo "Use 'bluetoothctl info <MAC>' for details"
echo "=========================================="
