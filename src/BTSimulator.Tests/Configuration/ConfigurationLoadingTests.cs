using System;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Xunit;
using BTSimulator.Demo.Configuration;

namespace BTSimulator.Tests.Configuration;

/// <summary>
/// Tests for configuration loading from appsettings.json.
/// </summary>
public class ConfigurationLoadingTests : IDisposable
{
    private readonly string _testDirectory;

    public ConfigurationLoadingTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"BTSimulator_ConfigTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);
    }

    [Fact]
    public void Configuration_LoadsFromJson_Successfully()
    {
        // Arrange
        var configJson = @"{
  ""Logging"": {
    ""LogDirectory"": ""test_logs"",
    ""MinLevel"": ""Debug""
  },
  ""Bluetooth"": {
    ""DeviceName"": ""Test Device"",
    ""DeviceAddress"": null,
    ""Services"": []
  }
}";
        var configFile = Path.Combine(_testDirectory, "appsettings.json");
        File.WriteAllText(configFile, configJson);

        // Act
        var configuration = new ConfigurationBuilder()
            .SetBasePath(_testDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        var settings = new AppSettings();
        configuration.Bind(settings);

        // Assert
        Assert.NotNull(settings);
        Assert.Equal("test_logs", settings.Logging.LogDirectory);
        Assert.Equal("Debug", settings.Logging.MinLevel);
        Assert.Equal("Test Device", settings.Bluetooth.DeviceName);
        // null in JSON gets bound as empty string
        Assert.True(string.IsNullOrEmpty(settings.Bluetooth.DeviceAddress));
        Assert.Empty(settings.Bluetooth.Services);
    }

    [Fact]
    public void Configuration_LoadsLoggingSettings_Correctly()
    {
        // Arrange
        var configJson = @"{
  ""Logging"": {
    ""LogDirectory"": ""my_logs"",
    ""MinLevel"": ""Warning""
  }
}";
        var configFile = Path.Combine(_testDirectory, "appsettings.json");
        File.WriteAllText(configFile, configJson);

        // Act
        var configuration = new ConfigurationBuilder()
            .SetBasePath(_testDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        var settings = new AppSettings();
        configuration.Bind(settings);

        // Assert
        Assert.Equal("my_logs", settings.Logging.LogDirectory);
        Assert.Equal("Warning", settings.Logging.MinLevel);
    }

    [Fact]
    public void Configuration_LoadsBluetoothSettings_Correctly()
    {
        // Arrange
        var configJson = @"{
  ""Bluetooth"": {
    ""DeviceName"": ""My BT Device"",
    ""DeviceAddress"": ""AA:BB:CC:DD:EE:FF"",
    ""Services"": []
  }
}";
        var configFile = Path.Combine(_testDirectory, "appsettings.json");
        File.WriteAllText(configFile, configJson);

        // Act
        var configuration = new ConfigurationBuilder()
            .SetBasePath(_testDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        var settings = new AppSettings();
        configuration.Bind(settings);

        // Assert
        Assert.Equal("My BT Device", settings.Bluetooth.DeviceName);
        Assert.Equal("AA:BB:CC:DD:EE:FF", settings.Bluetooth.DeviceAddress);
    }

    [Fact]
    public void Configuration_LoadsGattServices_Correctly()
    {
        // Arrange
        var configJson = @"{
  ""Bluetooth"": {
    ""DeviceName"": ""Test"",
    ""Services"": [
      {
        ""Uuid"": ""180F"",
        ""IsPrimary"": true,
        ""Characteristics"": [
          {
            ""Uuid"": ""2A19"",
            ""Flags"": [""read"", ""notify""],
            ""InitialValue"": ""55"",
            ""Description"": ""Battery level""
          }
        ]
      }
    ]
  }
}";
        var configFile = Path.Combine(_testDirectory, "appsettings.json");
        File.WriteAllText(configFile, configJson);

        // Act
        var configuration = new ConfigurationBuilder()
            .SetBasePath(_testDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        var settings = new AppSettings();
        configuration.Bind(settings);

        // Assert
        Assert.Single(settings.Bluetooth.Services);
        var service = settings.Bluetooth.Services[0];
        Assert.Equal("180F", service.Uuid);
        Assert.True(service.IsPrimary);
        
        Assert.Single(service.Characteristics);
        var characteristic = service.Characteristics[0];
        Assert.Equal("2A19", characteristic.Uuid);
        Assert.Equal(2, characteristic.Flags.Count);
        Assert.Contains("read", characteristic.Flags);
        Assert.Contains("notify", characteristic.Flags);
        Assert.Equal("55", characteristic.InitialValue);
        Assert.Equal("Battery level", characteristic.Description);
    }

    [Fact]
    public void Configuration_LoadsMultipleServices_Correctly()
    {
        // Arrange
        var configJson = @"{
  ""Bluetooth"": {
    ""DeviceName"": ""Test"",
    ""Services"": [
      {
        ""Uuid"": ""180F"",
        ""IsPrimary"": true,
        ""Characteristics"": []
      },
      {
        ""Uuid"": ""180A"",
        ""IsPrimary"": true,
        ""Characteristics"": []
      }
    ]
  }
}";
        var configFile = Path.Combine(_testDirectory, "appsettings.json");
        File.WriteAllText(configFile, configJson);

        // Act
        var configuration = new ConfigurationBuilder()
            .SetBasePath(_testDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        var settings = new AppSettings();
        configuration.Bind(settings);

        // Assert
        Assert.Equal(2, settings.Bluetooth.Services.Count);
        Assert.Equal("180F", settings.Bluetooth.Services[0].Uuid);
        Assert.Equal("180A", settings.Bluetooth.Services[1].Uuid);
    }

    [Fact]
    public void Configuration_LoadsMultipleCharacteristics_Correctly()
    {
        // Arrange
        var configJson = @"{
  ""Bluetooth"": {
    ""DeviceName"": ""Test"",
    ""Services"": [
      {
        ""Uuid"": ""180A"",
        ""IsPrimary"": true,
        ""Characteristics"": [
          {
            ""Uuid"": ""2A29"",
            ""Flags"": [""read""],
            ""InitialValue"": ""425453696D756C61746F72"",
            ""Description"": ""Manufacturer""
          },
          {
            ""Uuid"": ""2A24"",
            ""Flags"": [""read""],
            ""InitialValue"": ""44656D6F"",
            ""Description"": ""Model""
          }
        ]
      }
    ]
  }
}";
        var configFile = Path.Combine(_testDirectory, "appsettings.json");
        File.WriteAllText(configFile, configJson);

        // Act
        var configuration = new ConfigurationBuilder()
            .SetBasePath(_testDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        var settings = new AppSettings();
        configuration.Bind(settings);

        // Assert
        var service = settings.Bluetooth.Services[0];
        Assert.Equal(2, service.Characteristics.Count);
        Assert.Equal("2A29", service.Characteristics[0].Uuid);
        Assert.Equal("2A24", service.Characteristics[1].Uuid);
    }

    [Fact]
    public void Configuration_UsesDefaults_WhenSectionsAreMissing()
    {
        // Arrange
        var configJson = "{}";
        var configFile = Path.Combine(_testDirectory, "appsettings.json");
        File.WriteAllText(configFile, configJson);

        // Act
        var configuration = new ConfigurationBuilder()
            .SetBasePath(_testDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        var settings = new AppSettings();
        configuration.Bind(settings);

        // Assert - should use default values
        Assert.NotNull(settings.Logging);
        Assert.NotNull(settings.Bluetooth);
        Assert.Equal("logs", settings.Logging.LogDirectory);
        Assert.Equal("Info", settings.Logging.MinLevel);
        Assert.Equal("BT Simulator", settings.Bluetooth.DeviceName);
    }

    public void Dispose()
    {
        // Clean up test directory
        if (Directory.Exists(_testDirectory))
        {
            try
            {
                Directory.Delete(_testDirectory, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }
}
