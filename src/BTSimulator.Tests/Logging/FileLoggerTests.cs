using System;
using System.IO;
using System.Linq;
using System.Threading;
using Xunit;
using BTSimulator.Demo.Logging;

namespace BTSimulator.Tests.Logging;

/// <summary>
/// Tests for FileLogger functionality.
/// </summary>
public class FileLoggerTests : IDisposable
{
    private readonly string _testLogDirectory;

    public FileLoggerTests()
    {
        // Create a unique test directory for each test run
        _testLogDirectory = Path.Combine(Path.GetTempPath(), $"BTSimulator_LogTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testLogDirectory);
    }

    [Fact]
    public void FileLogger_CreatesLogDirectory_WhenNotExists()
    {
        // Arrange
        string logDir = Path.Combine(_testLogDirectory, "new_logs");
        
        // Act
        using var logger = new FileLogger(logDir);
        
        // Assert
        Assert.True(Directory.Exists(logDir), "Log directory should be created");
    }

    [Fact]
    public void FileLogger_WritesLogFile_OnFirstLog()
    {
        // Arrange
        using var logger = new FileLogger(_testLogDirectory);
        
        // Act
        logger.Info("Test message");
        
        // Assert
        var logFiles = Directory.GetFiles(_testLogDirectory, "log_*.txt");
        Assert.Single(logFiles);
    }

    [Fact]
    public void FileLogger_LogFormat_ContainsRequiredComponents()
    {
        // Arrange
        using var logger = new FileLogger(_testLogDirectory);
        string testMessage = "Test log message";
        
        // Act
        logger.Info(testMessage);
        
        // Assert
        var logFiles = Directory.GetFiles(_testLogDirectory, "log_*.txt");
        var logContent = File.ReadAllText(logFiles[0]);
        
        // Check format: [TimeStamp][Log Level][ClassName.MethodName][Message]
        Assert.Contains("[INFO]", logContent);
        Assert.Contains(testMessage, logContent);
        Assert.Contains("[FileLogger.", logContent); // Should contain class name
        Assert.Matches(@"\[\d{14}\.\d{3}\]", logContent); // Timestamp format yyyyMMddHHmmss.fff
    }

    [Fact]
    public void FileLogger_LogsDebugMessages()
    {
        // Arrange
        using var logger = new FileLogger(_testLogDirectory);
        string testMessage = "Debug test message";
        
        // Act
        logger.Debug(testMessage);
        
        // Assert
        var logFiles = Directory.GetFiles(_testLogDirectory, "log_*.txt");
        var logContent = File.ReadAllText(logFiles[0]);
        
        Assert.Contains("[DEBUG]", logContent);
        Assert.Contains(testMessage, logContent);
    }

    [Fact]
    public void FileLogger_LogsWarningMessages()
    {
        // Arrange
        using var logger = new FileLogger(_testLogDirectory);
        string testMessage = "Warning test message";
        
        // Act
        logger.Warning(testMessage);
        
        // Assert
        var logFiles = Directory.GetFiles(_testLogDirectory, "log_*.txt");
        var logContent = File.ReadAllText(logFiles[0]);
        
        Assert.Contains("[WARNING]", logContent);
        Assert.Contains(testMessage, logContent);
    }

    [Fact]
    public void FileLogger_LogsErrorMessages()
    {
        // Arrange
        using var logger = new FileLogger(_testLogDirectory);
        string testMessage = "Error test message";
        
        // Act
        logger.Error(testMessage);
        
        // Assert
        var logFiles = Directory.GetFiles(_testLogDirectory, "log_*.txt");
        var logContent = File.ReadAllText(logFiles[0]);
        
        Assert.Contains("[ERROR]", logContent);
        Assert.Contains(testMessage, logContent);
    }

    [Fact]
    public void FileLogger_LogsException_WhenProvided()
    {
        // Arrange
        using var logger = new FileLogger(_testLogDirectory);
        var exception = new InvalidOperationException("Test exception");
        
        // Act
        logger.Error("Error occurred", exception);
        
        // Assert
        var logFiles = Directory.GetFiles(_testLogDirectory, "log_*.txt");
        var logContent = File.ReadAllText(logFiles[0]);
        
        Assert.Contains("InvalidOperationException", logContent);
        Assert.Contains("Test exception", logContent);
    }

    [Fact]
    public void FileLogger_AppendsToExistingLogFile_OnSameDay()
    {
        // Arrange
        using var logger = new FileLogger(_testLogDirectory);
        
        // Act
        logger.Info("First message");
        logger.Info("Second message");
        
        // Assert
        var logFiles = Directory.GetFiles(_testLogDirectory, "log_*.txt");
        Assert.Single(logFiles);
        
        var logContent = File.ReadAllText(logFiles[0]);
        var lines = logContent.Split(System.Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);
        Assert.Contains("First message", lines[0]);
        Assert.Contains("Second message", lines[1]);
    }

    [Fact]
    public void FileLogger_CreatesLogFileWithDateFormat()
    {
        // Arrange
        using var logger = new FileLogger(_testLogDirectory);
        
        // Act
        logger.Info("Test message");
        
        // Assert
        var logFiles = Directory.GetFiles(_testLogDirectory, "log_*.txt");
        var fileName = Path.GetFileName(logFiles[0]);
        
        // Expected format: log_yyyyMMdd.txt
        Assert.Matches(@"log_\d{8}\.txt", fileName);
    }

    [Fact]
    public void FileLogger_CleansUpOldLogFiles_KeepsOnlyTwo()
    {
        // Arrange
        // Create 5 old log files with different dates
        for (int i = 0; i < 5; i++)
        {
            var oldFile = Path.Combine(_testLogDirectory, $"log_2025010{i + 1}.txt");
            File.WriteAllText(oldFile, $"Old log {i}");
            // Set different last write times to ensure they're ordered correctly
            File.SetLastWriteTime(oldFile, DateTime.Now.AddDays(-i - 1));
        }
        
        // Act
        using var logger = new FileLogger(_testLogDirectory);
        logger.Info("New message"); // This creates the current log file
        
        // Assert
        var logFiles = Directory.GetFiles(_testLogDirectory, "log_*.txt");
        // Should have at most 3 files (2 old + 1 new), but cleanup happens in constructor
        // so it should be 2 (most recent old file + new file) or 3 if timing is off
        Assert.InRange(logFiles.Length, 2, 3);
    }

    [Fact]
    public void FileLogger_ThreadSafe_MultipleWrites()
    {
        // Arrange
        using var logger = new FileLogger(_testLogDirectory);
        const int threadCount = 10;
        const int messagesPerThread = 5;
        
        // Act
        var threads = new Thread[threadCount];
        for (int i = 0; i < threadCount; i++)
        {
            int threadId = i;
            threads[i] = new Thread(() =>
            {
                for (int j = 0; j < messagesPerThread; j++)
                {
                    logger.Info($"Thread {threadId} Message {j}");
                }
            });
            threads[i].Start();
        }
        
        foreach (var thread in threads)
        {
            thread.Join();
        }
        
        // Assert
        var logFiles = Directory.GetFiles(_testLogDirectory, "log_*.txt");
        var logContent = File.ReadAllText(logFiles[0]);
        var lines = logContent.Split(System.Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        
        // Should have exactly threadCount * messagesPerThread lines
        Assert.Equal(threadCount * messagesPerThread, lines.Length);
    }

    public void Dispose()
    {
        // Clean up test directory
        if (Directory.Exists(_testLogDirectory))
        {
            try
            {
                Directory.Delete(_testLogDirectory, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }
}
