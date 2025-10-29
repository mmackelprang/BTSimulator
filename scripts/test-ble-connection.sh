#!/bin/bash
# BLE Connection Test Script
# Tests connecting to a BLE device and reading/writing characteristics
# Usage: ./test-ble-connection.sh <device_mac_address>

set -e

if [ -z "$1" ]; then
    echo "Usage: $0 <device_mac_address>"
    echo "Example: $0 AA:BB:CC:DD:EE:FF"
    exit 1
fi

DEVICE_MAC=$1

echo "=========================================="
echo "BLE Connection Test"
echo "=========================================="
echo "Target Device: $DEVICE_MAC"
echo ""

# Check if bluetoothctl is available
if ! command -v bluetoothctl &> /dev/null; then
    echo "Error: bluetoothctl not found. Please install bluez."
    exit 1
fi

# Check if gatttool is available
if ! command -v gatttool &> /dev/null; then
    echo "Warning: gatttool not found. Install bluez-deprecated for gatttool."
    echo "Continuing with bluetoothctl only..."
    USE_GATTTOOL=0
else
    USE_GATTTOOL=1
fi

echo "Step 1: Power on adapter"
bluetoothctl power on
sleep 1

echo "Step 2: Connecting to device..."
bluetoothctl connect "$DEVICE_MAC" &
CONNECT_PID=$!

# Wait for connection with timeout
TIMEOUT=15
COUNT=0
CONNECTED=0

while [ $COUNT -lt $TIMEOUT ]; do
    if bluetoothctl info "$DEVICE_MAC" | grep -q "Connected: yes"; then
        CONNECTED=1
        break
    fi
    sleep 1
    COUNT=$((COUNT + 1))
done

if [ $CONNECTED -eq 0 ]; then
    echo "Error: Failed to connect to device within $TIMEOUT seconds"
    exit 1
fi

echo "✓ Connected successfully"
echo ""

echo "Step 3: Reading device information..."
bluetoothctl info "$DEVICE_MAC"
echo ""

echo "Step 4: Discovering services..."
if [ $USE_GATTTOOL -eq 1 ]; then
    echo "Primary services:"
    gatttool -b "$DEVICE_MAC" --primary || true
    echo ""
    
    echo "Characteristics:"
    gatttool -b "$DEVICE_MAC" --characteristics || true
    echo ""
fi

echo "Step 5: Listing GATT attributes..."
bluetoothctl << EOF
menu gatt
list-attributes $DEVICE_MAC
back
EOF

echo ""
echo "Step 6: Disconnecting..."
bluetoothctl disconnect "$DEVICE_MAC"

wait $CONNECT_PID 2>/dev/null || true

echo ""
echo "=========================================="
echo "Connection test complete!"
echo "=========================================="
