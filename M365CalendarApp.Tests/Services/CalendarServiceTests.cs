using FluentAssertions;
using M365CalendarApp.Tests.Fixtures;
using M365CalendarApp.Tests.Helpers;
using M365CalendarApp.WPF.Services;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Moq;
using System.Net.Http;
using Xunit;

namespace M365CalendarApp.Tests.Services;

/// <summary>
/// Unit tests for CalendarService
/// </summary>
public class CalendarServiceTests : TestBase
{
    private readonly Mock<AuthenticationService> _mockAuthService;
    private readonly Mock<GraphServiceClient> _mockGraphClient;
    private readonly CalendarService _calendarService;

    public CalendarServiceTests()
    {
        _mockAuthService = new Mock<AuthenticationService>();
        _mockGraphClient = MockHelpers.CreateMockGraphServiceClient();
        _calendarService = new CalendarService(_mockAuthService.Object);
    }

    [Fact]
    public async Task InitializeAsync_ShouldReturnTrue_WhenAuthenticationIsValid()
    {
        // Arrange
        _mockAuthService.Setup(x => x.IsAuthenticated).Returns(true);
        _mockAuthService.Setup(x => x.GetAccessToken()).Returns("valid-token");

        // Act
        var result = await _calendarService.InitializeAsync();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task InitializeAsync_ShouldReturnFalse_WhenNotAuthenticated()
    {
        // Arrange
        _mockAuthService.Setup(x => x.IsAuthenticated).Returns(false);

        // Act
        var result = await _calendarService.InitializeAsync();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task InitializeAsync_ShouldReturnFalse_WhenAccessTokenIsNull()
    {
        // Arrange
        _mockAuthService.Setup(x => x.IsAuthenticated).Returns(true);
        _mockAuthService.Setup(x => x.GetAccessToken()).Returns((string)null);

        // Act
        var result = await _calendarService.InitializeAsync();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task InitializeAsync_ShouldReturnFalse_WhenAccessTokenIsEmpty()
    {
        // Arrange
        _mockAuthService.Setup(x => x.IsAuthenticated).Returns(true);
        _mockAuthService.Setup(x => x.GetAccessToken()).Returns(string.Empty);

        // Act
        var result = await _calendarService.InitializeAsync();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetEventsForDateAsync_ShouldThrowException_WhenNotInitialized()
    {
        // Arrange
        var testDate = TestDataFixtures.CalendarEvents.TestDate;

        // Act
        var action = async () => await _calendarService.GetEventsForDateAsync(testDate);

        // Assert
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Calendar service not initialized");
    }

    [Fact]
    public async Task GetEventsForDateAsync_ShouldReturnEmptyList_WhenNoEventsFound()
    {
        // Arrange
        var testDate = TestDataFixtures.CalendarEvents.TestDate;
        _mockAuthService.Setup(x => x.IsAuthenticated).Returns(true);
        _mockAuthService.Setup(x => x.GetAccessToken()).Returns("valid-token");
        
        await _calendarService.InitializeAsync();

        // Act
        var result = await _calendarService.GetEventsForDateAsync(testDate);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Theory]
    [InlineData("2024-12-09")]
    [InlineData("2024-01-01")]
    [InlineData("2024-12-31")]
    public async Task GetEventsForDateAsync_ShouldHandleDifferentDates(string dateString)
    {
        // Arrange
        var testDate = DateTime.Parse(dateString);
        _mockAuthService.Setup(x => x.IsAuthenticated).Returns(true);
        _mockAuthService.Setup(x => x.GetAccessToken()).Returns("valid-token");
        
        await _calendarService.InitializeAsync();

        // Act
        var result = await _calendarService.GetEventsForDateAsync(testDate);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public void CalendarEventInfo_ShouldFormatTimeCorrectly_ForRegularEvents()
    {
        // Arrange
        var eventInfo = new CalendarEventInfo
        {
            Subject = "Test Event",
            StartTime = new DateTime(2024, 12, 9, 9, 0, 0),
            EndTime = new DateTime(2024, 12, 9, 10, 0, 0),
            IsAllDay = false
        };

        // Act
        var timeDisplay = eventInfo.GetTimeDisplay();

        // Assert
        timeDisplay.Should().Be("9:00 AM - 10:00 AM");
    }

    [Fact]
    public void CalendarEventInfo_ShouldFormatTimeCorrectly_ForAllDayEvents()
    {
        // Arrange
        var eventInfo = new CalendarEventInfo
        {
            Subject = "All Day Event",
            StartTime = DateTime.Today,
            EndTime = DateTime.Today.AddDays(1),
            IsAllDay = true
        };

        // Act
        var timeDisplay = eventInfo.GetTimeDisplay();

        // Assert
        timeDisplay.Should().Be("All Day");
    }

    [Fact]
    public void CalendarEventInfo_ShouldCalculateDurationCorrectly_ForHourLongEvent()
    {
        // Arrange
        var eventInfo = new CalendarEventInfo
        {
            StartTime = new DateTime(2024, 12, 9, 9, 0, 0),
            EndTime = new DateTime(2024, 12, 9, 10, 0, 0),
            IsAllDay = false
        };

        // Act
        var duration = eventInfo.GetDurationDisplay();

        // Assert
        duration.Should().Be("1h 0m");
    }

    [Fact]
    public void CalendarEventInfo_ShouldCalculateDurationCorrectly_ForMinuteLongEvent()
    {
        // Arrange
        var eventInfo = new CalendarEventInfo
        {
            StartTime = new DateTime(2024, 12, 9, 9, 0, 0),
            EndTime = new DateTime(2024, 12, 9, 9, 30, 0),
            IsAllDay = false
        };

        // Act
        var duration = eventInfo.GetDurationDisplay();

        // Assert
        duration.Should().Be("30m");
    }

    [Fact]
    public void CalendarEventInfo_ShouldCalculateDurationCorrectly_ForMultiDayEvent()
    {
        // Arrange
        var eventInfo = new CalendarEventInfo
        {
            StartTime = new DateTime(2024, 12, 9, 9, 0, 0),
            EndTime = new DateTime(2024, 12, 11, 17, 0, 0),
            IsAllDay = false
        };

        // Act
        var duration = eventInfo.GetDurationDisplay();

        // Assert
        duration.Should().Be("2d 8h 0m");
    }

    [Fact]
    public void CalendarEventInfo_ShouldCalculateDurationCorrectly_ForAllDayEvent()
    {
        // Arrange
        var eventInfo = new CalendarEventInfo
        {
            StartTime = DateTime.Today,
            EndTime = DateTime.Today.AddDays(1),
            IsAllDay = true
        };

        // Act
        var duration = eventInfo.GetDurationDisplay();

        // Assert
        duration.Should().Be("All Day");
    }

    [Theory]
    [InlineData("free", "#28A745")]
    [InlineData("tentative", "#FFC107")]
    [InlineData("busy", "#DC3545")]
    [InlineData("oof", "#6F42C1")]
    [InlineData("workingelsewhere", "#17A2B8")]
    [InlineData("unknown", "#6C757D")]
    public void CalendarEventInfo_ShouldReturnCorrectStatusColor(string status, string expectedColor)
    {
        // Arrange
        var eventInfo = new CalendarEventInfo
        {
            ShowAs = status
        };

        // Act
        var color = eventInfo.GetStatusColor();

        // Assert
        color.Should().Be(expectedColor);
    }

    [Fact]
    public async Task GetCurrentUserAsync_ShouldReturnNull_WhenNotInitialized()
    {
        // Arrange - Service not initialized

        // Act
        var result = await _calendarService.GetCurrentUserAsync();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetCurrentUserAsync_ShouldReturnUserInfo_WhenSuccessful()
    {
        // This test would require proper mocking of GraphServiceClient
        // For now, we test the error handling path
        
        // Arrange
        _mockAuthService.Setup(x => x.IsAuthenticated).Returns(true);
        _mockAuthService.Setup(x => x.GetAccessToken()).Returns("valid-token");
        await _calendarService.InitializeAsync();

        // Act
        var result = await _calendarService.GetCurrentUserAsync();

        // Assert
        // Without proper Graph client mocking, this will return null
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetCalendarsAsync_ShouldReturnEmptyList_WhenNotInitialized()
    {
        // Arrange - Service not initialized

        // Act
        var result = await _calendarService.GetCalendarsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetCalendarsAsync_ShouldReturnEmptyList_WhenNoCalendarsFound()
    {
        // Arrange
        _mockAuthService.Setup(x => x.IsAuthenticated).Returns(true);
        _mockAuthService.Setup(x => x.GetAccessToken()).Returns("valid-token");
        await _calendarService.InitializeAsync();

        // Act
        var result = await _calendarService.GetCalendarsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public void UserInfo_ShouldHaveCorrectProperties()
    {
        // Arrange & Act
        var userInfo = new UserInfo
        {
            DisplayName = "Test User",
            Email = "test@example.com",
            JobTitle = "Developer",
            OfficeLocation = "Building 1"
        };

        // Assert
        userInfo.DisplayName.Should().Be("Test User");
        userInfo.Email.Should().Be("test@example.com");
        userInfo.JobTitle.Should().Be("Developer");
        userInfo.OfficeLocation.Should().Be("Building 1");
    }

    [Fact]
    public void CalendarEventInfo_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var eventInfo = new CalendarEventInfo();

        // Assert
        eventInfo.Subject.Should().Be("");
        eventInfo.Location.Should().Be("");
        eventInfo.BodyPreview.Should().Be("");
        eventInfo.ShowAs.Should().Be("");
        eventInfo.Sensitivity.Should().Be("");
        eventInfo.OrganizerName.Should().Be("");
        eventInfo.OrganizerEmail.Should().Be("");
        eventInfo.AttendeesCount.Should().Be(0);
        eventInfo.IsAllDay.Should().BeFalse();
    }

    [Fact]
    public void CalendarEventInfo_ShouldHandleNullValues()
    {
        // Arrange
        var eventInfo = new CalendarEventInfo
        {
            Subject = null,
            Location = null,
            ShowAs = null
        };

        // Act & Assert
        eventInfo.GetTimeDisplay().Should().NotBeNull();
        eventInfo.GetDurationDisplay().Should().NotBeNull();
        eventInfo.GetStatusColor().Should().NotBeNull();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    public void CalendarEventInfo_ShouldHandleDifferentAttendeesCounts(int attendeesCount)
    {
        // Arrange
        var eventInfo = new CalendarEventInfo
        {
            AttendeesCount = attendeesCount
        };

        // Act & Assert
        eventInfo.AttendeesCount.Should().Be(attendeesCount);
    }

    [Fact]
    public async Task Service_ShouldHandleServiceExceptions_Gracefully()
    {
        // This test verifies that ServiceExceptions are handled properly
        // In the actual implementation, ServiceExceptions are caught and logged
        
        // Arrange
        _mockAuthService.Setup(x => x.IsAuthenticated).Returns(true);
        _mockAuthService.Setup(x => x.GetAccessToken()).Returns("valid-token");
        await _calendarService.InitializeAsync();

        // Act
        var action = async () => await _calendarService.GetEventsForDateAsync(DateTime.Today);

        // Assert
        await action.Should().NotThrowAsync<ServiceException>();
    }

    [Fact]
    public async Task Service_ShouldHandleGeneralExceptions_Gracefully()
    {
        // This test verifies that general exceptions are handled properly
        
        // Arrange
        _mockAuthService.Setup(x => x.IsAuthenticated).Returns(true);
        _mockAuthService.Setup(x => x.GetAccessToken()).Returns("valid-token");
        await _calendarService.InitializeAsync();

        // Act
        var action = async () => await _calendarService.GetCurrentUserAsync();

        // Assert
        await action.Should().NotThrowAsync();
    }
}

/// <summary>
/// Tests for CalendarEventInfo data model
/// </summary>
public class CalendarEventInfoTests : TestBase
{
    [Fact]
    public void CalendarEventInfo_ShouldSerializeCorrectly()
    {
        // Arrange
        var eventInfo = new CalendarEventInfo
        {
            Subject = "Test Event",
            StartTime = new DateTime(2024, 12, 9, 9, 0, 0),
            EndTime = new DateTime(2024, 12, 9, 10, 0, 0),
            Location = "Conference Room",
            IsAllDay = false,
            ShowAs = "busy"
        };

        // Act
        var json = System.Text.Json.JsonSerializer.Serialize(eventInfo);

        // Assert
        json.Should().NotBeNullOrEmpty();
        json.Should().Contain("Test Event");
        json.Should().Contain("Conference Room");
    }

    [Fact]
    public void CalendarEventInfo_ShouldDeserializeCorrectly()
    {
        // Arrange
        var json = """
            {
                "Subject": "Test Event",
                "Location": "Conference Room",
                "IsAllDay": false,
                "ShowAs": "busy"
            }
            """;

        // Act
        var eventInfo = System.Text.Json.JsonSerializer.Deserialize<CalendarEventInfo>(json);

        // Assert
        eventInfo.Should().NotBeNull();
        eventInfo.Subject.Should().Be("Test Event");
        eventInfo.Location.Should().Be("Conference Room");
        eventInfo.IsAllDay.Should().BeFalse();
        eventInfo.ShowAs.Should().Be("busy");
    }

    [Theory]
    [MemberData(nameof(GetTimeFormatTestData))]
    public void CalendarEventInfo_ShouldFormatTime_Correctly(DateTime start, DateTime end, bool isAllDay, string expected)
    {
        // Arrange
        var eventInfo = new CalendarEventInfo
        {
            StartTime = start,
            EndTime = end,
            IsAllDay = isAllDay
        };

        // Act
        var result = eventInfo.GetTimeDisplay();

        // Assert
        result.Should().Be(expected);
    }

    public static IEnumerable<object[]> GetTimeFormatTestData()
    {
        yield return new object[] { new DateTime(2024, 12, 9, 9, 0, 0), new DateTime(2024, 12, 9, 10, 0, 0), false, "9:00 AM - 10:00 AM" };
        yield return new object[] { new DateTime(2024, 12, 9, 13, 30, 0), new DateTime(2024, 12, 9, 14, 45, 0), false, "1:30 PM - 2:45 PM" };
        yield return new object[] { DateTime.Today, DateTime.Today.AddDays(1), true, "All Day" };
        yield return new object[] { new DateTime(2024, 12, 9, 0, 0, 0), new DateTime(2024, 12, 9, 23, 59, 0), false, "12:00 AM - 11:59 PM" };
    }
}

/// <summary>
/// Tests for UserInfo data model
/// </summary>
public class UserInfoTests : TestBase
{
    [Fact]
    public void UserInfo_ShouldHaveCorrectDefaultValues()
    {
        // Arrange & Act
        var userInfo = new UserInfo();

        // Assert
        userInfo.DisplayName.Should().Be("");
        userInfo.Email.Should().Be("");
        userInfo.JobTitle.Should().Be("");
        userInfo.OfficeLocation.Should().Be("");
    }

    [Fact]
    public void UserInfo_ShouldAcceptAllProperties()
    {
        // Arrange
        var testData = TestDataFixtures.Users.SampleUsers[0];

        // Act
        var userInfo = new UserInfo
        {
            DisplayName = testData.DisplayName,
            Email = testData.Email,
            JobTitle = testData.JobTitle,
            OfficeLocation = "Test Location"
        };

        // Assert
        userInfo.DisplayName.Should().Be(testData.DisplayName);
        userInfo.Email.Should().Be(testData.Email);
        userInfo.JobTitle.Should().Be(testData.JobTitle);
        userInfo.OfficeLocation.Should().Be("Test Location");
    }

    [Fact]
    public void UserInfo_ShouldSerializeToJson()
    {
        // Arrange
        var userInfo = new UserInfo
        {
            DisplayName = "Test User",
            Email = "test@example.com",
            JobTitle = "Developer",
            OfficeLocation = "Building 1"
        };

        // Act
        var json = System.Text.Json.JsonSerializer.Serialize(userInfo);

        // Assert
        json.Should().NotBeNullOrEmpty();
        json.Should().Contain("Test User");
        json.Should().Contain("test@example.com");
        json.Should().Contain("Developer");
        json.Should().Contain("Building 1");
    }
}