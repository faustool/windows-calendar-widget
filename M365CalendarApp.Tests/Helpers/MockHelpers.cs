using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Identity.Client;
using Moq;
using System.Net.Http;

namespace M365CalendarApp.Tests.Helpers;

/// <summary>
/// Helper class for creating common mocks used across tests
/// </summary>
public static class MockHelpers
{
    /// <summary>
    /// Creates a mock IPublicClientApplication for authentication tests
    /// </summary>
    public static Mock<IPublicClientApplication> CreateMockPublicClientApplication()
    {
        var mock = new Mock<IPublicClientApplication>();
        
        // Setup default behavior
        mock.Setup(x => x.GetAccountsAsync())
            .ReturnsAsync(new List<IAccount>());
            
        return mock;
    }

    /// <summary>
    /// Creates a mock AuthenticationResult for successful authentication
    /// </summary>
    public static AuthenticationResult CreateMockAuthenticationResult(
        string accessToken = "mock-access-token",
        string username = "test@example.com")
    {
        var account = new Mock<IAccount>();
        account.Setup(x => x.Username).Returns(username);
        
        // Note: AuthenticationResult is sealed, so we need to use reflection or create a wrapper
        // For testing purposes, we'll create a test double
        return CreateAuthenticationResultTestDouble(accessToken, account.Object);
    }

    /// <summary>
    /// Creates a test double for AuthenticationResult since it's sealed
    /// </summary>
    private static AuthenticationResult CreateAuthenticationResultTestDouble(string accessToken, IAccount account)
    {
        // This is a simplified approach - in real scenarios you might use a wrapper interface
        // For now, we'll return null and handle this in the actual service tests
        return null!; // Will be handled properly in individual tests
    }

    /// <summary>
    /// Creates a mock GraphServiceClient
    /// </summary>
    public static Mock<GraphServiceClient> CreateMockGraphServiceClient()
    {
        var httpClient = new HttpClient();
        var mock = new Mock<GraphServiceClient>(httpClient);
        return mock;
    }

    /// <summary>
    /// Creates mock calendar events for testing
    /// </summary>
    public static List<Event> CreateMockCalendarEvents(int count = 3)
    {
        var events = new List<Event>();
        var baseDate = DateTime.Today;

        for (int i = 0; i < count; i++)
        {
            var startTime = baseDate.AddHours(9 + i * 2);
            var endTime = startTime.AddHours(1);

            events.Add(new Event
            {
                Id = $"event-{i + 1}",
                Subject = $"Test Event {i + 1}",
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
                Location = new Location
                {
                    DisplayName = $"Room {i + 1}"
                },
                BodyPreview = $"This is test event {i + 1}",
                IsAllDay = false,
                ShowAs = FreeBusyStatus.Busy,
                Sensitivity = Sensitivity.Normal,
                Organizer = new Recipient
                {
                    EmailAddress = new EmailAddress
                    {
                        Name = "Test Organizer",
                        Address = "organizer@example.com"
                    }
                },
                Attendees = new List<Attendee>
                {
                    new Attendee
                    {
                        EmailAddress = new EmailAddress
                        {
                            Name = "Test Attendee",
                            Address = "attendee@example.com"
                        }
                    }
                }
            });
        }

        return events;
    }

    /// <summary>
    /// Creates a mock User for testing
    /// </summary>
    public static User CreateMockUser(
        string displayName = "Test User",
        string email = "test@example.com",
        string jobTitle = "Software Developer")
    {
        return new User
        {
            Id = "user-123",
            DisplayName = displayName,
            Mail = email,
            UserPrincipalName = email,
            JobTitle = jobTitle,
            OfficeLocation = "Building 1"
        };
    }

    /// <summary>
    /// Creates a mock HttpClient for testing HTTP operations
    /// </summary>
    public static HttpClient CreateMockHttpClient()
    {
        var handler = new Mock<HttpMessageHandler>();
        return new HttpClient(handler.Object);
    }

    /// <summary>
    /// Sets up a mock to throw a specific exception
    /// </summary>
    public static void SetupMockToThrow<T>(Mock<T> mock, string methodName, Exception exception) where T : class
    {
        // This will be implemented in specific test classes based on the method signatures
    }
}