using System;
using BTSimulator.Core.Logging;

namespace BTSimulator.Scanner;

/// <summary>
/// Simple console logger for the scanner utility
/// Only outputs warnings and errors to keep the console clean
/// </summary>
public class ConsoleLogger : ILogger
{
    public void Debug(string message, Exception? exception = null)
    {
        // Don't output debug messages to keep console clean
    }

    public void Info(string message, Exception? exception = null)
    {
        // Don't output info messages to keep console clean
    }

    public void Warning(string message, Exception? exception = null)
    {
        Console.WriteLine($"Warning: {message}");
        if (exception != null)
        {
            Console.WriteLine($"  Details: {exception.Message}");
        }
    }

    public void Error(string message, Exception? exception = null)
    {
        Console.WriteLine($"Error: {message}");
        if (exception != null)
        {
            Console.WriteLine($"  Details: {exception.Message}");
        }
    }
}
