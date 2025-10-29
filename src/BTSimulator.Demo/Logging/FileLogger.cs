using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using BTSimulator.Core.Logging;

namespace BTSimulator.Demo.Logging;

/// <summary>
/// Custom file logger implementation that writes to daily log files with automatic rotation and cleanup.
/// Log format: [TimeStamp yyyyMMddHHmmss.fff][Log Level][ClassName.MethodName][Message][Exception (optional)]
/// </summary>
public class FileLogger : BTSimulator.Core.Logging.ILogger, IDisposable
{
    private readonly string _logDirectory;
    private readonly object _lockObject = new object();
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the FileLogger.
    /// </summary>
    /// <param name="logDirectory">Directory where log files will be stored.</param>
    public FileLogger(string logDirectory)
    {
        _logDirectory = logDirectory ?? throw new ArgumentNullException(nameof(logDirectory));
        
        // Create log directory if it doesn't exist
        if (!Directory.Exists(_logDirectory))
        {
            Directory.CreateDirectory(_logDirectory);
        }

        // Perform cleanup of old log files (keep only 2 most recent)
        CleanupOldLogFiles();
    }

    /// <inheritdoc/>
    public void Debug(string message, Exception? exception = null)
    {
        WriteLog("DEBUG", message, exception);
    }

    /// <inheritdoc/>
    public void Info(string message, Exception? exception = null)
    {
        WriteLog("INFO", message, exception);
    }

    /// <inheritdoc/>
    public void Warning(string message, Exception? exception = null)
    {
        WriteLog("WARNING", message, exception);
    }

    /// <inheritdoc/>
    public void Error(string message, Exception? exception = null)
    {
        WriteLog("ERROR", message, exception);
    }

    private void WriteLog(string level, string message, Exception? exception, [CallerMemberName] string callerMethod = "", [CallerFilePath] string callerFile = "")
    {
        if (_disposed)
            return;

        try
        {
            // Get caller class name from file path
            string className = Path.GetFileNameWithoutExtension(callerFile);
            
            // Format: [TimeStamp yyyyMMddHHmmss.fff][Log Level][ClassName.MethodName][Message][Exception (optional)]
            string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss.fff");
            string logEntry = $"[{timestamp}][{level}][{className}.{callerMethod}][{message}]";
            
            if (exception != null)
            {
                logEntry += $"[{exception.GetType().Name}: {exception.Message}";
                if (exception.StackTrace != null)
                {
                    logEntry += $"\nStack Trace: {exception.StackTrace}";
                }
                logEntry += "]";
            }

            // Get today's log file path
            string logFilePath = GetLogFilePath();

            // Write to file (thread-safe)
            lock (_lockObject)
            {
                File.AppendAllText(logFilePath, logEntry + Environment.NewLine);
            }
        }
        catch
        {
            // Silently fail - logging should not crash the application
        }
    }

    private string GetLogFilePath()
    {
        // Log file name format: log_yyyyMMdd.txt
        string fileName = $"log_{DateTime.Now:yyyyMMdd}.txt";
        return Path.Combine(_logDirectory, fileName);
    }

    private void CleanupOldLogFiles()
    {
        try
        {
            var logFiles = Directory.GetFiles(_logDirectory, "log_*.txt")
                .Select(f => new FileInfo(f))
                .OrderByDescending(f => f.LastWriteTime)
                .ToList();

            // Keep only the 2 most recent log files
            if (logFiles.Count > 2)
            {
                foreach (var fileToDelete in logFiles.Skip(2))
                {
                    try
                    {
                        fileToDelete.Delete();
                    }
                    catch
                    {
                        // Ignore errors during cleanup
                    }
                }
            }
        }
        catch
        {
            // Silently fail - cleanup should not crash the application
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
        }
    }
}
