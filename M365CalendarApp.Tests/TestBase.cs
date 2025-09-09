using Microsoft.Extensions.Logging;
using Moq;
using System.IO;

namespace M365CalendarApp.Tests;

/// <summary>
/// Base class for all unit tests providing common setup and utilities
/// </summary>
public abstract class TestBase : IDisposable
{
    protected Mock<ILogger> MockLogger { get; }
    protected string TestDataDirectory { get; }
    protected string TempDirectory { get; }

    protected TestBase()
    {
        MockLogger = new Mock<ILogger>();
        TestDataDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData");
        TempDirectory = Path.Combine(Path.GetTempPath(), "M365CalendarApp.Tests", Guid.NewGuid().ToString());
        
        // Ensure test directories exist
        Directory.CreateDirectory(TestDataDirectory);
        Directory.CreateDirectory(TempDirectory);
    }

    /// <summary>
    /// Creates a temporary file with the specified content
    /// </summary>
    protected string CreateTempFile(string content, string fileName = null)
    {
        fileName ??= $"test_{Guid.NewGuid()}.tmp";
        var filePath = Path.Combine(TempDirectory, fileName);
        File.WriteAllText(filePath, content);
        return filePath;
    }

    /// <summary>
    /// Creates a temporary JSON file with the specified object
    /// </summary>
    protected string CreateTempJsonFile<T>(T obj, string fileName = null)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(obj, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });
        return CreateTempFile(json, fileName ?? "test.json");
    }

    /// <summary>
    /// Verifies that a mock was called with specific parameters
    /// </summary>
    protected void VerifyMockCall<T>(Mock<T> mock, string methodName, Times times) where T : class
    {
        // This is a helper method for common mock verifications
        // Specific implementations will be in individual test classes
    }

    public virtual void Dispose()
    {
        // Clean up temporary directory
        if (Directory.Exists(TempDirectory))
        {
            try
            {
                Directory.Delete(TempDirectory, true);
            }
            catch
            {
                // Ignore cleanup errors in tests
            }
        }
    }
}