#!/bin/bash
# BLE Read/Write Test Script
# Tests reading and writing to GATT characteristics
# Usage: ./test-read-write.sh <device_mac> <characteristic_handle> [value_to_write]

set -e

if [ -z "$1" ] || [ -z "$2" ]; then
    echo "Usage: $0 <device_mac> <characteristic_handle> [value_to_write]"
    echo "Example (read):  $0 AA:BB:CC:DD:EE:FF 0x0010"
    echo "Example (write): $0 AA:BB:CC:DD:EE:FF 0x0010 \"48656c6c6f\""
    echo ""
    echo "Note: Use handle values (e.g., 0x0010), not UUIDs"
    echo "      To find handles, use: gatttool -b <MAC> --characteristics"
    exit 1
fi

DEVICE_MAC=$1
CHAR_HANDLE=$2
WRITE_VALUE=$3

echo "=========================================="
echo "BLE Read/Write Test"
echo "=========================================="
echo "Device: $DEVICE_MAC"
echo "Characteristic Handle: $CHAR_HANDLE"
if [ -n "$WRITE_VALUE" ]; then
    echo "Write value: $WRITE_VALUE"
fi
echo ""

# Check dependencies
if ! command -v gatttool &> /dev/null; then
    echo "Error: gatttool not found."
    echo "Install with: sudo apt-get install bluez-deprecated"
    exit 1
fi

echo "Step 1: Connecting to device..."
# Use gatttool in interactive mode for better control
if [ -n "$WRITE_VALUE" ]; then
    # Write test
    echo "Step 2: Writing value to characteristic..."
    gatttool -b "$DEVICE_MAC" --char-write-req --handle="$CHAR_HANDLE" --value="$WRITE_VALUE"
    
    if [ $? -eq 0 ]; then
        echo "✓ Write successful"
    else
        echo "✗ Write failed"
        exit 1
    fi
    
    echo ""
    echo "Step 3: Reading back value..."
    READ_VALUE=$(gatttool -b "$DEVICE_MAC" --char-read --handle="$CHAR_HANDLE" 2>&1)
    echo "Read value: $READ_VALUE"
else
    # Read test only
    echo "Step 2: Reading characteristic value..."
    READ_VALUE=$(gatttool -b "$DEVICE_MAC" --char-read --handle="$CHAR_HANDLE" 2>&1)
    
    if [ $? -eq 0 ]; then
        echo "✓ Read successful"
        echo "Value: $READ_VALUE"
    else
        echo "✗ Read failed"
        echo "Error: $READ_VALUE"
        exit 1
    fi
fi

echo ""
echo "=========================================="
echo "Read/Write test complete!"
echo "=========================================="
