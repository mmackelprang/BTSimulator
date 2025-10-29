# BlueZ D-Bus API Mapping

This document maps the C# BTSimulator API to the underlying BlueZ D-Bus interfaces.

## Overview

BTSimulator uses Tmds.DBus to communicate with BlueZ over D-Bus. This document serves as a reference for understanding how C# classes map to BlueZ D-Bus objects and interfaces.

## Architecture

```
BTSimulator Application
        ↓
   Tmds.DBus Library
        ↓
   D-Bus System Bus
        ↓
   BlueZ Daemon (bluetoothd)
        ↓
  Bluetooth Adapter (HCI)
```

## Core Components

### BlueZManager ↔ org.bluez Service

The `BlueZManager` class manages the connection to the BlueZ service.

**C# Class**: `BTSimulator.Core.BlueZ.BlueZManager`

**D-Bus Service**: `org.bluez`

**D-Bus Object Path**: `/` (root)

#### Key Methods

| C# Method | D-Bus Interface | D-Bus Method | Description |
|-----------|-----------------|--------------|-------------|
| `ConnectAsync()` | - | - | Establishes D-Bus system bus connection |
| `GetAdaptersAsync()` | `org.freedesktop.DBus.ObjectManager` | `GetManagedObjects` | Discovers Bluetooth adapters |
| `CreateAdapter(path)` | - | - | Creates adapter proxy for specified path |
| `IsBlueZAvailableAsync()` | `org.freedesktop.DBus` | `ListNames` | Checks if BlueZ service is running |

**BlueZ Reference**: [BlueZ API Overview](https://git.kernel.org/pub/scm/bluetooth/bluez.git/tree/doc)

---

### BlueZAdapter ↔ org.bluez.Adapter1

The `BlueZAdapter` class represents a Bluetooth adapter (typically hci0).

**C# Class**: `BTSimulator.Core.BlueZ.BlueZAdapter`

**D-Bus Interface**: `org.bluez.Adapter1`

**D-Bus Object Path**: `/org/bluez/hci0` (example)

#### Properties

| C# Property/Method | D-Bus Property | Type | Access | Description |
|-------------------|----------------|------|--------|-------------|
| `GetAddressAsync()` | `Address` | string | read | Bluetooth MAC address |
| `GetNameAsync()` | `Name` | string | read | Adapter system name |
| `SetAliasAsync(alias)` | `Alias` | string | read/write | Human-readable adapter name |
| `GetPoweredAsync()` | `Powered` | boolean | read/write | Power state |
| `SetPoweredAsync(powered)` | `Powered` | boolean | read/write | Power on/off adapter |
| `SetDiscoverableAsync(discoverable)` | `Discoverable` | boolean | read/write | Discoverable state |

#### Additional Adapter Properties (Available in BlueZ)

| D-Bus Property | Type | Description |
|----------------|------|-------------|
| `Pairable` | boolean | Pairable state |
| `PairableTimeout` | uint32 | Pairable timeout in seconds |
| `DiscoverableTimeout` | uint32 | Discoverable timeout in seconds |
| `Discovering` | boolean | Currently discovering devices |
| `UUIDs` | array{string} | Supported service UUIDs |
| `Modalias` | string | Device model identifier |

**BlueZ Reference**: [adapter-api.txt](https://git.kernel.org/pub/scm/bluetooth/bluez.git/tree/doc/adapter-api.txt)

---

### LE Advertising ↔ org.bluez.LEAdvertisingManager1

Manages BLE advertisements for peripheral mode.

**D-Bus Interface**: `org.bluez.LEAdvertisingManager1`

**D-Bus Object Path**: Same as adapter (e.g., `/org/bluez/hci0`)

#### Methods

| Operation | D-Bus Method | Parameters | Description |
|-----------|--------------|------------|-------------|
| Register Advertisement | `RegisterAdvertisement` | `object path`, `dict options` | Register a BLE advertisement |
| Unregister Advertisement | `UnregisterAdvertisement` | `object path` | Remove advertisement |

**BlueZ Reference**: [advertising-api.txt](https://git.kernel.org/pub/scm/bluetooth/bluez.git/tree/doc/advertising-api.txt)

---

### LE Advertisement ↔ org.bluez.LEAdvertisement1

Represents a BLE advertisement to be broadcast.

**D-Bus Interface**: `org.bluez.LEAdvertisement1`

**Custom Object Path**: Application-defined (e.g., `/com/example/advertisement0`)

#### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Type` | string | Advertisement type: "broadcast", "peripheral" |
| `ServiceUUIDs` | array{string} | List of service UUIDs to advertise |
| `ManufacturerData` | dict{uint16, array{byte}} | Manufacturer-specific data |
| `ServiceData` | dict{string, array{byte}} | Service-specific data |
| `LocalName` | string | Local device name |
| `Includes` | array{string} | List of features to include |

#### Methods

| Method | Description |
|--------|-------------|
| `Release` | Called when advertisement is released |

**Example Advertisement Configuration**:
```json
{
  "Type": "peripheral",
  "ServiceUUIDs": ["180F"],
  "LocalName": "BT Simulator",
  "Includes": ["tx-power"]
}
```

---

### GATT Management ↔ org.bluez.GattManager1

Manages GATT services for peripheral mode.

**D-Bus Interface**: `org.bluez.GattManager1`

**D-Bus Object Path**: Same as adapter (e.g., `/org/bluez/hci0`)

#### Methods

| Operation | D-Bus Method | Parameters | Description |
|-----------|--------------|------------|-------------|
| Register Application | `RegisterApplication` | `object path`, `dict options` | Register GATT services |
| Unregister Application | `UnregisterApplication` | `object path` | Remove GATT services |

**BlueZ Reference**: [gatt-api.txt](https://git.kernel.org/pub/scm/bluetooth/bluez.git/tree/doc/gatt-api.txt)

---

### GATT Service ↔ org.bluez.GattService1

Represents a GATT service with characteristics.

**D-Bus Interface**: `org.bluez.GattService1`

**Custom Object Path**: Application-defined (e.g., `/com/example/service0`)

#### Properties

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `UUID` | string | Yes | 128-bit service UUID |
| `Primary` | boolean | Yes | True for primary service |
| `Characteristics` | array{object} | No | List of characteristic paths |

**Standard Service UUIDs**:
- `180F`: Battery Service
- `180A`: Device Information Service
- `1800`: Generic Access Service
- `1801`: Generic Attribute Service

---

### GATT Characteristic ↔ org.bluez.GattCharacteristic1

Represents a GATT characteristic with read/write/notify capabilities.

**D-Bus Interface**: `org.bluez.GattCharacteristic1`

**Custom Object Path**: Application-defined (e.g., `/com/example/service0/char0`)

#### Properties

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `UUID` | string | Yes | 128-bit characteristic UUID |
| `Service` | object | Yes | Parent service object path |
| `Flags` | array{string} | Yes | Characteristic properties |
| `Value` | array{byte} | No | Current value |
| `Descriptors` | array{object} | No | List of descriptor paths |

#### Flags (Properties)

| Flag | Description |
|------|-------------|
| `read` | Characteristic can be read |
| `write` | Characteristic can be written with response |
| `write-without-response` | Characteristic can be written without response |
| `notify` | Characteristic supports notifications |
| `indicate` | Characteristic supports indications |
| `broadcast` | Characteristic supports broadcast |
| `authenticated-signed-writes` | Requires authenticated signed writes |

#### Methods

| Method | Parameters | Description |
|--------|------------|-------------|
| `ReadValue` | `dict options` | Called when client reads value |
| `WriteValue` | `array{byte} value`, `dict options` | Called when client writes value |
| `StartNotify` | - | Start sending notifications |
| `StopNotify` | - | Stop sending notifications |

**Standard Characteristic UUIDs**:
- `2A19`: Battery Level
- `2A29`: Manufacturer Name String
- `2A24`: Model Number String
- `2A00`: Device Name

---

## Device Configuration Mapping

### DeviceConfiguration ↔ BlueZ Properties

The `DeviceConfiguration` class maps to various BlueZ properties:

| C# Property | BlueZ Property/Feature | Notes |
|-------------|------------------------|-------|
| `DeviceName` | Adapter `Alias` + Advertisement `LocalName` | Sets both adapter alias and advertised name |
| `DeviceAddress` | Adapter `Address` | Read-only in most cases; requires special permissions |
| `Services` | GATT Application | Registered via `GattManager1.RegisterApplication` |

### GattServiceConfiguration ↔ org.bluez.GattService1

| C# Property | D-Bus Property | Notes |
|-------------|----------------|-------|
| `Uuid` | `UUID` | 16-bit or 128-bit UUID |
| `IsPrimary` | `Primary` | Primary vs secondary service |
| `Characteristics` | `Characteristics` | List of characteristic object paths |

### GattCharacteristicConfiguration ↔ org.bluez.GattCharacteristic1

| C# Property | D-Bus Property | Notes |
|-------------|----------------|-------|
| `Uuid` | `UUID` | 16-bit or 128-bit UUID |
| `Flags` | `Flags` | Properties: read, write, notify, etc. |
| `InitialValue` | `Value` | Initial characteristic value |

---

## D-Bus Message Flow Examples

### Example 1: Setting Adapter Alias

1. **C# Call**:
   ```csharp
   await adapter.SetAliasAsync("My BLE Device");
   ```

2. **D-Bus Message**:
   ```
   Method Call:
     Destination: org.bluez
     Path: /org/bluez/hci0
     Interface: org.freedesktop.DBus.Properties
     Method: Set
     Parameters:
       - "org.bluez.Adapter1"
       - "Alias"
       - Variant("My BLE Device")
   ```

### Example 2: Registering GATT Service

1. **C# Setup**:
   ```csharp
   var service = new GattServiceConfiguration {
       Uuid = "180F",  // Battery Service
       IsPrimary = true
   };
   ```

2. **D-Bus Registration**:
   ```
   Method Call:
     Destination: org.bluez
     Path: /org/bluez/hci0
     Interface: org.bluez.GattManager1
     Method: RegisterApplication
     Parameters:
       - "/com/btsimulator/app0"
       - {}  // options dictionary
   ```

3. **D-Bus Object Structure**:
   ```
   /com/btsimulator/app0
     └─ /com/btsimulator/app0/service0
          └─ /com/btsimulator/app0/service0/char0
   ```

---

## Implementation Status

| Component | Status | Notes |
|-----------|--------|-------|
| D-Bus Connection | ✅ Complete | Basic connection established |
| Adapter Discovery | 🚧 In Progress | ObjectManager implementation needed |
| Adapter Properties | 🚧 In Progress | Properties interface implementation needed |
| Advertisement Registration | ⏳ Planned | Phase 4 |
| GATT Service Registration | ⏳ Planned | Phase 4 |
| Characteristic Read/Write | ⏳ Planned | Phase 4 |

Legend:
- ✅ Complete
- 🚧 In Progress
- ⏳ Planned
- ❌ Not Implemented

---

## Code Examples

### Using BlueZManager

```csharp
using BTSimulator.Core.BlueZ;

// Connect to BlueZ
var manager = new BlueZManager();
if (!await manager.ConnectAsync())
{
    Console.WriteLine("Failed to connect to BlueZ");
    return;
}

// Get default adapter
var adapterPath = await manager.GetDefaultAdapterAsync();
if (adapterPath == null)
{
    Console.WriteLine("No Bluetooth adapter found");
    return;
}

// Create adapter instance
var adapter = manager.CreateAdapter(adapterPath);

// Get adapter information
var address = await adapter.GetAddressAsync();
Console.WriteLine($"Adapter Address: {address}");

// Set adapter alias
await adapter.SetAliasAsync("BTSimulator Device");

// Power on adapter
await adapter.SetPoweredAsync(true);
```

---

## References

### Official BlueZ Documentation

- [BlueZ Project](http://www.bluez.org/)
- [BlueZ Git Repository](https://git.kernel.org/pub/scm/bluetooth/bluez.git)
- [BlueZ D-Bus API Documentation](https://git.kernel.org/pub/scm/bluetooth/bluez.git/tree/doc)

### D-Bus Documentation

- [D-Bus Specification](https://dbus.freedesktop.org/doc/dbus-specification.html)
- [D-Bus Tutorial](https://dbus.freedesktop.org/doc/dbus-tutorial.html)
- [Tmds.DBus GitHub](https://github.com/tmds/Tmds.DBus)

### Bluetooth Specifications

- [Bluetooth SIG](https://www.bluetooth.com/)
- [GATT Specifications](https://www.bluetooth.com/specifications/gatt/)
- [Assigned Numbers](https://www.bluetooth.com/specifications/assigned-numbers/)

---

## Troubleshooting D-Bus Communication

### View D-Bus Messages

Use `dbus-monitor` to watch D-Bus traffic:

```bash
# Monitor system bus (requires root or appropriate permissions)
sudo dbus-monitor --system

# Filter for BlueZ messages
sudo dbus-monitor --system "type='signal',sender='org.bluez'"
```

### Introspect BlueZ Objects

Use `dbus-send` to introspect:

```bash
# List all BlueZ objects
dbus-send --system --print-reply --dest=org.bluez / org.freedesktop.DBus.ObjectManager.GetManagedObjects

# Introspect adapter
dbus-send --system --print-reply --dest=org.bluez /org/bluez/hci0 org.freedesktop.DBus.Introspectable.Introspect

# Get adapter properties
dbus-send --system --print-reply --dest=org.bluez /org/bluez/hci0 org.freedesktop.DBus.Properties.GetAll string:"org.bluez.Adapter1"
```

### Test with Python

Quick test using Python and `pydbus`:

```python
from pydbus import SystemBus

bus = SystemBus()
adapter = bus.get('org.bluez', '/org/bluez/hci0')

print(f"Address: {adapter.Address}")
print(f"Name: {adapter.Name}")
adapter.Alias = "Test Device"
print(f"Alias: {adapter.Alias}")
```
