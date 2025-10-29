using System.Threading.Tasks;
using Xunit;
using BTSimulator.Core.Environment;

namespace BTSimulator.Tests.Environment;

/// <summary>
/// Tests for environment verification functionality.
/// These tests verify the detection of Linux, WSL2, BlueZ, D-Bus, and permissions.
/// </summary>
public class EnvironmentVerifierTests
{
    private readonly EnvironmentVerifier _verifier;

    public EnvironmentVerifierTests()
    {
        _verifier = new EnvironmentVerifier();
    }

    [Fact]
    public void IsLinuxEnvironment_ShouldReturnBoolean()
    {
        // Act
        var result = _verifier.IsLinuxEnvironment();

        // Assert
        // Result should be a valid boolean (true on Linux, false otherwise)
        Assert.IsType<bool>(result);
    }

    [Fact]
    public async Task IsWSL2Environment_ShouldReturnBoolean()
    {
        // Act
        var result = await _verifier.IsWSL2Environment();

        // Assert
        Assert.IsType<bool>(result);
        
        // If not Linux, should definitely not be WSL2
        if (!_verifier.IsLinuxEnvironment())
        {
            Assert.False(result);
        }
    }

    [Fact]
    public async Task VerifyBlueZInstallation_ShouldReturnResult()
    {
        // Act
        var result = await _verifier.VerifyBlueZInstallation();

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Version);
        
        // If any component is found, IsInstalled should be true
        if (result.BluetoothDaemonFound || result.BluetoothCtlFound || result.HciConfigFound)
        {
            Assert.True(result.IsInstalled);
        }
    }

    [Fact]
    public async Task VerifyDBusConnectivity_ShouldReturnBoolean()
    {
        // Act
        var result = await _verifier.VerifyDBusConnectivity();

        // Assert
        Assert.IsType<bool>(result);
    }

    [Fact]
    public async Task VerifyPermissions_ShouldReturnResult()
    {
        // Act
        var result = await _verifier.VerifyPermissions();

        // Assert
        Assert.NotNull(result);
        
        // If root, should have sufficient permissions
        if (result.IsRoot)
        {
            Assert.True(result.HasSufficientPermissions);
        }
        
        // If in bluetooth group, should have sufficient permissions
        if (result.InBluetoothGroup)
        {
            Assert.True(result.HasSufficientPermissions);
        }
    }

    [Fact]
    public async Task VerifyEnvironment_ShouldReturnCompleteResult()
    {
        // Act
        var result = await _verifier.VerifyEnvironment();

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.BlueZResult);
        Assert.NotNull(result.PermissionResult);
        
        // IsReady should only be true if all requirements are met
        if (result.IsReady)
        {
            Assert.True(result.IsLinux);
            Assert.True(result.BlueZResult.IsInstalled);
            Assert.True(result.HasDBusAccess);
            Assert.True(result.PermissionResult.HasSufficientPermissions);
        }
    }

    [Fact]
    public async Task GetSummary_ShouldReturnFormattedString()
    {
        // Arrange
        var result = await _verifier.VerifyEnvironment();

        // Act
        var summary = result.GetSummary();

        // Assert
        Assert.NotNull(summary);
        Assert.Contains("Environment Verification Summary", summary);
        Assert.Contains("Platform:", summary);
        Assert.Contains("BlueZ Installed:", summary);
        Assert.Contains("D-Bus Access:", summary);
        Assert.Contains("Sufficient Permissions:", summary);
    }

    [Fact]
    public async Task BlueZVerificationResult_ShouldHaveConsistentState()
    {
        // Act
        var result = await _verifier.VerifyBlueZInstallation();

        // Assert
        // If BlueZ is installed, at least one component should be found
        if (result.IsInstalled)
        {
            Assert.True(result.BluetoothDaemonFound || result.BluetoothCtlFound || result.HciConfigFound);
        }
        
        // If no components are found, IsInstalled should be false
        if (!result.BluetoothDaemonFound && !result.BluetoothCtlFound && !result.HciConfigFound)
        {
            Assert.False(result.IsInstalled);
        }
    }
}
