using Microsoft.Graph.Models;
using System.IO;
using System.Text.Json;

namespace M365CalendarApp.Tests.Fixtures;

/// <summary>
/// Utility class for loading test data from JSON files
/// </summary>
public static class TestDataLoader
{
    private static readonly string TestDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData");

    /// <summary>
    /// Loads sample calendar events from JSON file
    /// </summary>
    public static async Task<List<Event>> LoadSampleEventsAsync()
    {
        var filePath = Path.Combine(TestDataPath, "sample-events.json");
        if (!File.Exists(filePath))
        {
            return new List<Event>();
        }

        var json = await File.ReadAllTextAsync(filePath);
        var events = JsonSerializer.Deserialize<List<Event>>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return events ?? new List<Event>();
    }

    /// <summary>
    /// Loads all-day events from JSON file
    /// </summary>
    public static async Task<List<Event>> LoadAllDayEventsAsync()
    {
        var filePath = Path.Combine(TestDataPath, "all-day-events.json");
        if (!File.Exists(filePath))
        {
            return new List<Event>();
        }

        var json = await File.ReadAllTextAsync(filePath);
        var events = JsonSerializer.Deserialize<List<Event>>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return events ?? new List<Event>();
    }

    /// <summary>
    /// Loads sample user data from JSON file
    /// </summary>
    public static async Task<User?> LoadSampleUserAsync()
    {
        var filePath = Path.Combine(TestDataPath, "sample-user.json");
        if (!File.Exists(filePath))
        {
            return null;
        }

        var json = await File.ReadAllTextAsync(filePath);
        var user = JsonSerializer.Deserialize<User>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return user;
    }

    /// <summary>
    /// Loads error scenarios for testing
    /// </summary>
    public static async Task<ErrorScenariosData?> LoadErrorScenariosAsync()
    {
        var filePath = Path.Combine(TestDataPath, "error-scenarios.json");
        if (!File.Exists(filePath))
        {
            return null;
        }

        var json = await File.ReadAllTextAsync(filePath);
        var errorScenarios = JsonSerializer.Deserialize<ErrorScenariosData>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return errorScenarios;
    }

    /// <summary>
    /// Creates a mock calendar event with specified parameters
    /// </summary>
    public static Event CreateMockEvent(
        string subject,
        DateTime startTime,
        DateTime endTime,
        string location = "",
        bool isAllDay = false,
        string showAs = "busy")
    {
        return new Event
        {
            Id = Guid.NewGuid().ToString(),
            Subject = subject,
            Start = new DateTimeTimeZone
            {
                DateTime = startTime.ToString("yyyy-MM-ddTHH:mm:ss.fffK"),
                TimeZone = "UTC"
            },
            End = new DateTimeTimeZone
            {
                DateTime = endTime.ToString("yyyy-MM-ddTHH:mm:ss.fffK"),
                TimeZone = "UTC"
            },
            Location = string.IsNullOrEmpty(location) ? null : new Location { DisplayName = location },
            IsAllDay = isAllDay,
            ShowAs = Enum.TryParse<FreeBusyStatus>(showAs, true, out var status) ? status : FreeBusyStatus.Busy,
            Sensitivity = Sensitivity.Normal,
            BodyPreview = $"Test event: {subject}",
            Organizer = new Recipient
            {
                EmailAddress = new EmailAddress
                {
                    Name = "Test Organizer",
                    Address = "organizer@test.com"
                }
            },
            Attendees = new List<Attendee>
            {
                new Attendee
                {
                    EmailAddress = new EmailAddress
                    {
                        Name = "Test Attendee",
                        Address = "attendee@test.com"
                    }
                }
            }
        };
    }

    /// <summary>
    /// Creates a collection of events for a specific date
    /// </summary>
    public static List<Event> CreateEventsForDate(DateTime date, int eventCount = 3)
    {
        var events = new List<Event>();
        var baseTime = date.Date.AddHours(9); // Start at 9 AM

        for (int i = 0; i < eventCount; i++)
        {
            var startTime = baseTime.AddHours(i * 2);
            var endTime = startTime.AddHours(1);

            events.Add(CreateMockEvent(
                $"Test Event {i + 1}",
                startTime,
                endTime,
                $"Room {i + 1}",
                false,
                i % 2 == 0 ? "busy" : "tentative"
            ));
        }

        return events;
    }

    /// <summary>
    /// Creates a mock user for testing
    /// </summary>
    public static User CreateMockUser(
        string displayName = "Test User",
        string email = "test@example.com",
        string jobTitle = "Software Developer")
    {
        return new User
        {
            Id = Guid.NewGuid().ToString(),
            DisplayName = displayName,
            Mail = email,
            UserPrincipalName = email,
            JobTitle = jobTitle,
            OfficeLocation = "Building 1",
            Department = "Engineering",
            CompanyName = "Test Company"
        };
    }

    /// <summary>
    /// Validates that test data files exist
    /// </summary>
    public static bool ValidateTestDataFiles()
    {
        var requiredFiles = new[]
        {
            "sample-events.json",
            "sample-user.json",
            "all-day-events.json",
            "error-scenarios.json"
        };

        return requiredFiles.All(file => File.Exists(Path.Combine(TestDataPath, file)));
    }

    /// <summary>
    /// Gets the test data directory path
    /// </summary>
    public static string GetTestDataPath() => TestDataPath;
}

/// <summary>
/// Data structure for error scenarios
/// </summary>
public class ErrorScenariosData
{
    public List<ErrorScenario> Scenarios { get; set; } = new();
    public ErrorTestData TestData { get; set; } = new();
}

/// <summary>
/// Individual error scenario
/// </summary>
public class ErrorScenario
{
    public string Name { get; set; } = "";
    public string ErrorType { get; set; } = "";
    public string ErrorMessage { get; set; } = "";
    public int? HttpStatusCode { get; set; }
    public bool ShouldRetry { get; set; }
    public string ExpectedBehavior { get; set; } = "";
}

/// <summary>
/// Test data for error responses
/// </summary>
public class ErrorTestData
{
    public object? InvalidTokenResponse { get; set; }
    public object? RateLimitResponse { get; set; }
    public object? ForbiddenResponse { get; set; }
}