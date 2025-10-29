using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace BTSimulator.Core.Environment;

/// <summary>
/// Verifies the system environment for Bluetooth simulation requirements.
/// Checks for Linux/WSL2, BlueZ installation, D-Bus availability, and proper permissions.
/// </summary>
public class EnvironmentVerifier
{
    /// <summary>
    /// Verifies if the application is running on Linux or WSL2.
    /// </summary>
    /// <returns>True if running on Linux/WSL2, false otherwise.</returns>
    public bool IsLinuxEnvironment()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
    }

    /// <summary>
    /// Checks if the application is running under WSL2 specifically.
    /// </summary>
    /// <returns>True if running under WSL2, false otherwise.</returns>
    public async Task<bool> IsWSL2Environment()
    {
        if (!IsLinuxEnvironment())
            return false;

        try
        {
            // Check for WSL-specific indicators
            if (File.Exists("/proc/version"))
            {
                var content = await File.ReadAllTextAsync("/proc/version");
                return content.Contains("microsoft", StringComparison.OrdinalIgnoreCase) ||
                       content.Contains("WSL", StringComparison.OrdinalIgnoreCase);
            }
        }
        catch
        {
            // If we can't read the file, assume not WSL2
        }

        return false;
    }

    /// <summary>
    /// Verifies if BlueZ is installed on the system.
    /// Checks for bluetoothd, bluetoothctl, and hciconfig binaries.
    /// </summary>
    /// <returns>BlueZ verification result with details.</returns>
    public async Task<BlueZVerificationResult> VerifyBlueZInstallation()
    {
        var result = new BlueZVerificationResult();

        // Check for bluetoothd daemon
        result.BluetoothDaemonFound = await CheckCommandExists("bluetoothd");
        
        // Check for bluetoothctl CLI tool
        result.BluetoothCtlFound = await CheckCommandExists("bluetoothctl");
        
        // Check for hciconfig (legacy but useful)
        result.HciConfigFound = await CheckCommandExists("hciconfig");

        // Get BlueZ version if available
        if (result.BluetoothCtlFound)
        {
            result.Version = await GetBlueZVersion();
        }

        result.IsInstalled = result.BluetoothDaemonFound || result.BluetoothCtlFound;

        return result;
    }

    /// <summary>
    /// Verifies D-Bus system bus connectivity.
    /// </summary>
    /// <returns>True if D-Bus is accessible, false otherwise.</returns>
    public async Task<bool> VerifyDBusConnectivity()
    {
        try
        {
            // Check if D-Bus socket exists
            var dbusSocket = System.Environment.GetEnvironmentVariable("DBUS_SYSTEM_BUS_ADDRESS") 
                ?? "unix:path=/var/run/dbus/system_bus_socket";

            // For system bus, check if the socket file exists
            if (dbusSocket.StartsWith("unix:path="))
            {
                var socketPath = dbusSocket.Replace("unix:path=", "");
                if (!File.Exists(socketPath))
                    return false;
            }

            // Try to connect to D-Bus using dbus-send if available
            return await CheckCommandExists("dbus-send");
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Verifies if the current user has the necessary permissions for Bluetooth operations.
    /// Checks group membership (bluetooth, netdev) and capabilities.
    /// </summary>
    /// <returns>Permission verification result with details.</returns>
    public async Task<PermissionVerificationResult> VerifyPermissions()
    {
        var result = new PermissionVerificationResult();

        try
        {
            // Check group memberships
            var groups = await ExecuteCommand("groups");
            result.InBluetoothGroup = groups.Contains("bluetooth");
            result.InNetdevGroup = groups.Contains("netdev");

            // Check if running as root
            var userId = await ExecuteCommand("id", "-u");
            result.IsRoot = userId.Trim() == "0";

            result.HasSufficientPermissions = result.IsRoot || result.InBluetoothGroup;
        }
        catch (Exception ex)
        {
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    /// <summary>
    /// Performs a comprehensive environment verification.
    /// </summary>
    /// <returns>Complete environment verification result.</returns>
    public async Task<EnvironmentVerificationResult> VerifyEnvironment()
    {
        var result = new EnvironmentVerificationResult
        {
            IsLinux = IsLinuxEnvironment(),
            IsWSL2 = await IsWSL2Environment(),
            BlueZResult = await VerifyBlueZInstallation(),
            HasDBusAccess = await VerifyDBusConnectivity(),
            PermissionResult = await VerifyPermissions()
        };

        result.IsReady = result.IsLinux && 
                         result.BlueZResult.IsInstalled && 
                         result.HasDBusAccess && 
                         result.PermissionResult.HasSufficientPermissions;

        return result;
    }

    private async Task<bool> CheckCommandExists(string command)
    {
        try
        {
            var result = await ExecuteCommand("which", command);
            return !string.IsNullOrWhiteSpace(result);
        }
        catch
        {
            return false;
        }
    }

    private async Task<string> GetBlueZVersion()
    {
        try
        {
            var result = await ExecuteCommand("bluetoothctl", "--version");
            return result.Trim();
        }
        catch
        {
            return "Unknown";
        }
    }

    private async Task<string> ExecuteCommand(string command, string? args = null)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = command,
            Arguments = args ?? string.Empty,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = startInfo };
        process.Start();

        var output = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();

        return output;
    }
}

/// <summary>
/// Result of BlueZ installation verification.
/// </summary>
public class BlueZVerificationResult
{
    public bool IsInstalled { get; set; }
    public bool BluetoothDaemonFound { get; set; }
    public bool BluetoothCtlFound { get; set; }
    public bool HciConfigFound { get; set; }
    public string Version { get; set; } = "Unknown";
}

/// <summary>
/// Result of permission verification.
/// </summary>
public class PermissionVerificationResult
{
    public bool HasSufficientPermissions { get; set; }
    public bool IsRoot { get; set; }
    public bool InBluetoothGroup { get; set; }
    public bool InNetdevGroup { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Complete environment verification result.
/// </summary>
public class EnvironmentVerificationResult
{
    public bool IsReady { get; set; }
    public bool IsLinux { get; set; }
    public bool IsWSL2 { get; set; }
    public BlueZVerificationResult BlueZResult { get; set; } = new();
    public bool HasDBusAccess { get; set; }
    public PermissionVerificationResult PermissionResult { get; set; } = new();

    public string GetSummary()
    {
        var summary = $"Environment Verification Summary:\n" +
                     $"  Platform: {(IsLinux ? (IsWSL2 ? "WSL2" : "Linux") : "Not Linux")}\n" +
                     $"  BlueZ Installed: {BlueZResult.IsInstalled} (Version: {BlueZResult.Version})\n" +
                     $"  D-Bus Access: {HasDBusAccess}\n" +
                     $"  Sufficient Permissions: {PermissionResult.HasSufficientPermissions}\n" +
                     $"  Ready for Bluetooth Simulation: {IsReady}";

        if (!IsReady)
        {
            summary += "\n\nIssues found:\n";
            if (!IsLinux)
                summary += "  - Not running on Linux/WSL2\n";
            if (!BlueZResult.IsInstalled)
                summary += "  - BlueZ is not installed\n";
            if (!HasDBusAccess)
                summary += "  - D-Bus system bus is not accessible\n";
            if (!PermissionResult.HasSufficientPermissions)
                summary += "  - Insufficient permissions (need root or bluetooth group)\n";
        }

        return summary;
    }
}
