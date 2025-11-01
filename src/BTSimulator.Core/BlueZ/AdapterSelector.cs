using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BTSimulator.Core.Logging;

namespace BTSimulator.Core.BlueZ;

/// <summary>
/// Provides adapter selection functionality with fallback to console prompts.
/// </summary>
public class AdapterSelector
{
    private readonly BlueZManager _manager;
    private readonly ILogger _logger;

    public AdapterSelector(BlueZManager manager, ILogger? logger = null)
    {
        _manager = manager ?? throw new ArgumentNullException(nameof(manager));
        _logger = logger ?? NullLogger.Instance;
    }

    /// <summary>
    /// Selects an adapter based on configuration or prompts user if not configured.
    /// </summary>
    /// <param name="configuredAdapterName">Optional adapter name from configuration (e.g., "hci0" or "/org/bluez/hci0").</param>
    /// <param name="promptIfMissing">Whether to prompt user if configuration is missing.</param>
    /// <returns>Selected adapter path or null if selection failed.</returns>
    public async Task<string?> SelectAdapterAsync(string? configuredAdapterName = null, bool promptIfMissing = true)
    {
        var adapters = await _manager.GetAdapterInfosAsync();

        if (adapters.Count == 0)
        {
            _logger.Error("No Bluetooth adapters found");
            return null;
        }

        // If only one adapter, use it
        if (adapters.Count == 1)
        {
            _logger.Info($"Using only available adapter: {adapters[0]}");
            return adapters[0].Path;
        }

        // Try to use configured adapter
        if (!string.IsNullOrEmpty(configuredAdapterName))
        {
            var selectedAdapter = FindAdapterByName(adapters, configuredAdapterName);
            if (selectedAdapter != null)
            {
                _logger.Info($"Using configured adapter: {selectedAdapter}");
                return selectedAdapter.Path;
            }

            _logger.Warning($"Configured adapter '{configuredAdapterName}' not found");
        }

        // Prompt user if enabled
        if (promptIfMissing)
        {
            return await PromptForAdapterAsync(adapters);
        }

        // Default to first adapter
        _logger.Info($"Using default adapter: {adapters[0]}");
        return adapters[0].Path;
    }

    /// <summary>
    /// Finds an adapter by name or path.
    /// </summary>
    private static AdapterInfo? FindAdapterByName(List<AdapterInfo> adapters, string nameOrPath)
    {
        // Try exact path match first
        var adapter = adapters.FirstOrDefault(a => a.Path.Equals(nameOrPath, StringComparison.OrdinalIgnoreCase));
        if (adapter != null)
            return adapter;

        // Try name match
        adapter = adapters.FirstOrDefault(a => a.Name.Equals(nameOrPath, StringComparison.OrdinalIgnoreCase));
        if (adapter != null)
            return adapter;

        // Try partial path match (e.g., "hci0" matches "/org/bluez/hci0")
        return adapters.FirstOrDefault(a => a.Path.EndsWith("/" + nameOrPath, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Prompts the user to select an adapter from available options.
    /// Note: This method uses direct Console I/O for interactive user selection.
    /// It is intentionally synchronous and console-coupled as it's designed for
    /// command-line application use cases where user interaction is required.
    /// </summary>
    private Task<string?> PromptForAdapterAsync(List<AdapterInfo> adapters)
    {
        Console.WriteLine();
        Console.WriteLine("Multiple Bluetooth adapters detected. Please select one:");
        Console.WriteLine();

        for (int i = 0; i < adapters.Count; i++)
        {
            var adapter = adapters[i];
            var poweredStatus = adapter.Powered ? "ON" : "OFF";
            Console.WriteLine($"  [{i + 1}] {adapter.Name} - {adapter.Address}");
            Console.WriteLine($"      Alias: {adapter.Alias}, Powered: {poweredStatus}");
        }

        Console.WriteLine();
        Console.Write($"Enter selection (1-{adapters.Count}) or press Enter for default [{adapters[0].Name}]: ");

        var input = Console.ReadLine();
        
        if (string.IsNullOrWhiteSpace(input))
        {
            _logger.Info($"Using default adapter: {adapters[0]}");
            return Task.FromResult<string?>(adapters[0].Path);
        }

        if (int.TryParse(input, out var selection) && selection >= 1 && selection <= adapters.Count)
        {
            var selectedAdapter = adapters[selection - 1];
            _logger.Info($"User selected adapter: {selectedAdapter}");
            return Task.FromResult<string?>(selectedAdapter.Path);
        }

        Console.WriteLine("Invalid selection. Using default adapter.");
        _logger.Warning("Invalid adapter selection, using default");
        return Task.FromResult<string?>(adapters[0].Path);
    }
}
