using System;

namespace BTSimulator.Core.Logging;

/// <summary>
/// A no-op logger implementation that discards all log messages.
/// Useful for testing or when logging is not needed.
/// </summary>
public class NullLogger : ILogger
{
    /// <summary>
    /// Gets a singleton instance of the NullLogger.
    /// </summary>
    public static NullLogger Instance { get; } = new NullLogger();

    private NullLogger() { }

    /// <inheritdoc/>
    public void Debug(string message, Exception? exception = null) { }

    /// <inheritdoc/>
    public void Info(string message, Exception? exception = null) { }

    /// <inheritdoc/>
    public void Warning(string message, Exception? exception = null) { }

    /// <inheritdoc/>
    public void Error(string message, Exception? exception = null) { }
}
