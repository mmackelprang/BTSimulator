using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace BTSimulator.Core.Device;

/// <summary>
/// Handles persistence of device configurations to and from JSON files.
/// </summary>
public class DeviceConfigurationPersistence
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>
    /// Saves a device configuration to a JSON file.
    /// </summary>
    /// <param name="configuration">The configuration to save.</param>
    /// <param name="filePath">The path to save the configuration to.</param>
    public static async Task SaveToFileAsync(DeviceConfiguration configuration, string filePath)
    {
        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));

        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be empty", nameof(filePath));

        // Validate configuration before saving
        if (!configuration.Validate(out var errors))
        {
            throw new InvalidOperationException($"Configuration is invalid: {string.Join(", ", errors)}");
        }

        try
        {
            var json = SerializeConfiguration(configuration);
            await File.WriteAllTextAsync(filePath, json);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to save configuration to {filePath}", ex);
        }
    }

    /// <summary>
    /// Loads a device configuration from a JSON file.
    /// </summary>
    /// <param name="filePath">The path to load the configuration from.</param>
    /// <returns>The loaded device configuration.</returns>
    public static async Task<DeviceConfiguration> LoadFromFileAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be empty", nameof(filePath));

        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Configuration file not found: {filePath}");

        try
        {
            var json = await File.ReadAllTextAsync(filePath);
            var configuration = DeserializeConfiguration(json);

            // Validate loaded configuration
            if (!configuration.Validate(out var errors))
            {
                throw new InvalidOperationException($"Loaded configuration is invalid: {string.Join(", ", errors)}");
            }

            return configuration;
        }
        catch (Exception ex) when (ex is not FileNotFoundException and not InvalidOperationException)
        {
            throw new InvalidOperationException($"Failed to load configuration from {filePath}", ex);
        }
    }

    /// <summary>
    /// Serializes a device configuration to JSON string.
    /// </summary>
    /// <param name="configuration">The configuration to serialize.</param>
    /// <returns>JSON string representation of the configuration.</returns>
    public static string SerializeConfiguration(DeviceConfiguration configuration)
    {
        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));

        var dto = new DeviceConfigurationDto
        {
            DeviceName = configuration.DeviceName,
            DeviceAddress = configuration.DeviceAddress,
            Services = configuration.Services.Select(s => new GattServiceDto
            {
                Uuid = s.Uuid,
                IsPrimary = s.IsPrimary,
                Characteristics = s.Characteristics.Select(c => new GattCharacteristicDto
                {
                    Uuid = c.Uuid,
                    Flags = c.Flags,
                    InitialValue = Convert.ToBase64String(c.InitialValue),
                    Description = c.Description
                }).ToList()
            }).ToList()
        };

        return JsonSerializer.Serialize(dto, JsonOptions);
    }

    /// <summary>
    /// Deserializes a device configuration from JSON string.
    /// </summary>
    /// <param name="json">JSON string containing the configuration.</param>
    /// <returns>Deserialized device configuration.</returns>
    public static DeviceConfiguration DeserializeConfiguration(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new ArgumentException("JSON cannot be empty", nameof(json));

        try
        {
            var dto = JsonSerializer.Deserialize<DeviceConfigurationDto>(json, JsonOptions);
            if (dto == null)
                throw new InvalidOperationException("Failed to deserialize configuration");

            var configuration = new DeviceConfiguration
            {
                DeviceName = dto.DeviceName ?? "BT Simulator",
                DeviceAddress = dto.DeviceAddress
            };

            if (dto.Services != null)
            {
                foreach (var serviceDto in dto.Services)
                {
                    var service = new GattServiceConfiguration
                    {
                        Uuid = serviceDto.Uuid ?? string.Empty,
                        IsPrimary = serviceDto.IsPrimary
                    };

                    if (serviceDto.Characteristics != null)
                    {
                        foreach (var charDto in serviceDto.Characteristics)
                        {
                            var characteristic = new GattCharacteristicConfiguration
                            {
                                Uuid = charDto.Uuid ?? string.Empty,
                                Flags = charDto.Flags ?? new List<string>(),
                                InitialValue = !string.IsNullOrEmpty(charDto.InitialValue)
                                    ? Convert.FromBase64String(charDto.InitialValue)
                                    : Array.Empty<byte>(),
                                Description = charDto.Description ?? string.Empty
                            };

                            service.AddCharacteristic(characteristic);
                        }
                    }

                    configuration.AddService(service);
                }
            }

            return configuration;
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("Invalid JSON format", ex);
        }
    }

    // DTOs for JSON serialization
    private class DeviceConfigurationDto
    {
        public string? DeviceName { get; set; }
        public string? DeviceAddress { get; set; }
        public List<GattServiceDto>? Services { get; set; }
    }

    private class GattServiceDto
    {
        public string? Uuid { get; set; }
        public bool IsPrimary { get; set; }
        public List<GattCharacteristicDto>? Characteristics { get; set; }
    }

    private class GattCharacteristicDto
    {
        public string? Uuid { get; set; }
        public List<string>? Flags { get; set; }
        public string? InitialValue { get; set; } // Base64 encoded
        public string? Description { get; set; }
    }
}
