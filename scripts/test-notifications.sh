#!/bin/bash
# BLE Notification Test Script
# Tests GATT characteristic notifications
# Usage: ./test-notifications.sh <device_mac> <characteristic_handle> [duration_seconds]

set -e

if [ -z "$1" ] || [ -z "$2" ]; then
    echo "Usage: $0 <device_mac> <characteristic_handle> [duration_seconds]"
    echo "Example: $0 AA:BB:CC:DD:EE:FF 0x0010 30"
    exit 1
fi

DEVICE_MAC=$1
CHAR_HANDLE=$2
DURATION=${3:-30}

echo "=========================================="
echo "BLE Notification Test"
echo "=========================================="
echo "Device: $DEVICE_MAC"
echo "Characteristic Handle: $CHAR_HANDLE"
echo "Duration: $DURATION seconds"
echo ""

# Check dependencies
if ! command -v gatttool &> /dev/null; then
    echo "Error: gatttool not found."
    echo "Install with: sudo apt-get install bluez-deprecated"
    exit 1
fi

echo "Step 1: Connecting to device..."
echo ""

# Create expect script for notification handling
EXPECT_SCRIPT=$(mktemp)
cat > "$EXPECT_SCRIPT" << 'EXPECT_EOF'
#!/usr/bin/expect -f

set timeout -1
set device_mac [lindex $argv 0]
set char_handle [lindex $argv 1]
set duration [lindex $argv 2]

spawn gatttool -b $device_mac -I

expect "\\[LE\\]>" {
    send "connect\r"
}

expect "Connection successful" {
    puts "\n✓ Connected successfully\n"
    send "char-write-req $char_handle 0100\r"
}

expect "Characteristic value was written successfully" {
    puts "✓ Notifications enabled (wrote 0x0100 to CCCD)\n"
    puts "Listening for notifications (press Ctrl+C to stop)...\n"
}

# Listen for notifications
set timeout $duration
expect {
    "Notification handle" {
        puts "Received: $expect_out(buffer)"
        exp_continue
    }
    timeout {
        puts "\nDuration elapsed."
    }
}

send "disconnect\r"
expect "\\[LE\\]>"
send "exit\r"
expect eof
EXPECT_EOF

chmod +x "$EXPECT_SCRIPT"

# Check if expect is installed
if ! command -v expect &> /dev/null; then
    echo "Error: expect not found."
    echo "Install with: sudo apt-get install expect"
    rm "$EXPECT_SCRIPT"
    exit 1
fi

# Run the expect script
expect "$EXPECT_SCRIPT" "$DEVICE_MAC" "$CHAR_HANDLE" "$DURATION"

# Cleanup
rm "$EXPECT_SCRIPT"

echo ""
echo "=========================================="
echo "Notification test complete!"
echo "=========================================="
