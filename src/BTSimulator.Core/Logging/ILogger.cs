using System;

namespace BTSimulator.Core.Logging;

/// <summary>
/// Interface for logging within BTSimulator.Core.
/// Implementations should provide the specific logging format and output mechanism.
/// </summary>
public interface ILogger
{
    /// <summary>
    /// Logs a debug message.
    /// Debug messages are typically used for detailed diagnostic information.
    /// </summary>
    /// <param name="message">The message to log.</param>
    /// <param name="exception">Optional exception to include.</param>
    void Debug(string message, Exception? exception = null);

    /// <summary>
    /// Logs an informational message.
    /// Info messages are typically used for general application flow.
    /// </summary>
    /// <param name="message">The message to log.</param>
    /// <param name="exception">Optional exception to include.</param>
    void Info(string message, Exception? exception = null);

    /// <summary>
    /// Logs a warning message.
    /// Warning messages indicate potential issues that don't prevent operation.
    /// </summary>
    /// <param name="message">The message to log.</param>
    /// <param name="exception">Optional exception to include.</param>
    void Warning(string message, Exception? exception = null);

    /// <summary>
    /// Logs an error message.
    /// Error messages indicate failures or exceptional conditions.
    /// </summary>
    /// <param name="message">The message to log.</param>
    /// <param name="exception">Optional exception to include.</param>
    void Error(string message, Exception? exception = null);
}
