using System.Collections.Generic;
using System.Threading.Tasks;

namespace BTSimulator.Core.BlueZ;

/// <summary>
/// Placeholder interfaces for BlueZ D-Bus API.
/// 
/// Note: Tmds.DBus.Protocol uses a different approach than the legacy Tmds.DBus library.
/// These interfaces are defined for documentation purposes and to specify the BlueZ API contract.
/// Actual D-Bus communication will be handled through raw message passing.
/// 
/// References:
/// - Adapter API: https://git.kernel.org/pub/scm/bluetooth/bluez.git/tree/doc/adapter-api.txt
/// - Advertising API: https://git.kernel.org/pub/scm/bluetooth/bluez.git/tree/doc/advertising-api.txt
/// - GATT API: https://git.kernel.org/pub/scm/bluetooth/bluez.git/tree/doc/gatt-api.txt
/// </summary>
public static class BlueZConstants
{
    public const string Service = "org.bluez";
    public const string Adapter1Interface = "org.bluez.Adapter1";
    public const string LEAdvertisingManager1Interface = "org.bluez.LEAdvertisingManager1";
    public const string GattManager1Interface = "org.bluez.GattManager1";
    public const string LEAdvertisement1Interface = "org.bluez.LEAdvertisement1";
    public const string GattService1Interface = "org.bluez.GattService1";
    public const string GattCharacteristic1Interface = "org.bluez.GattCharacteristic1";
    public const string ObjectManagerInterface = "org.freedesktop.DBus.ObjectManager";
    public const string PropertiesInterface = "org.freedesktop.DBus.Properties";
}
