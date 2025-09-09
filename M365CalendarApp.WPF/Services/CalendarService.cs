using Microsoft.Graph;
using Microsoft.Graph.Models;
using System.Net.Http;
using System.Net.Http.Headers;

namespace M365CalendarApp.WPF.Services;

public class CalendarService
{
    private GraphServiceClient? _graphServiceClient;
    private readonly AuthenticationService _authService;

    public CalendarService(AuthenticationService authService)
    {
        _authService = authService;
    }

    public async Task<bool> InitializeAsync()
    {
        try
        {
            if (!_authService.IsAuthenticated)
            {
                return false;
            }

            var accessToken = _authService.GetAccessToken();
            if (string.IsNullOrEmpty(accessToken))
            {
                return false;
            }

            // Create HTTP client with proxy support
            var httpClient = new HttpClient(ProxyService.CreateHttpClientHandler());
            httpClient.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", accessToken);

            _graphServiceClient = new GraphServiceClient(httpClient);
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Calendar service initialization failed: {ex.Message}");
            return false;
        }
    }

    public async Task<List<CalendarEventInfo>> GetEventsForDateAsync(DateTime date)
    {
        var events = new List<CalendarEventInfo>();

        try
        {
            if (_graphServiceClient == null)
            {
                throw new InvalidOperationException("Calendar service not initialized");
            }

            var startTime = date.Date;
            var endTime = date.Date.AddDays(1);

            var calendarView = await _graphServiceClient.Me.Calendar.CalendarView
                .GetAsync(requestConfiguration =>
                {
                    requestConfiguration.QueryParameters.StartDateTime = startTime.ToString("yyyy-MM-ddTHH:mm:ss.fffK");
                    requestConfiguration.QueryParameters.EndDateTime = endTime.ToString("yyyy-MM-ddTHH:mm:ss.fffK");
                    requestConfiguration.QueryParameters.Orderby = new[] { "start/dateTime" };
                    requestConfiguration.QueryParameters.Select = new[] 
                    { 
                        "subject", 
                        "start", 
                        "end", 
                        "location", 
                        "bodyPreview", 
                        "isAllDay",
                        "showAs",
                        "sensitivity",
                        "organizer",
                        "attendees"
                    };
                });

            if (calendarView?.Value != null)
            {
                foreach (var eventItem in calendarView.Value)
                {
                    events.Add(ConvertToCalendarEventInfo(eventItem));
                }
            }
        }
        catch (ServiceException ex)
        {
            System.Diagnostics.Debug.WriteLine($"Graph API error: {ex.ResponseStatusCode} - {ex.Message}");
            throw new Exception($"Failed to retrieve calendar events: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Calendar service error: {ex.Message}");
            throw;
        }

        return events;
    }

    public async Task<UserInfo?> GetCurrentUserAsync()
    {
        try
        {
            if (_graphServiceClient == null)
            {
                throw new InvalidOperationException("Calendar service not initialized");
            }

            var user = await _graphServiceClient.Me.GetAsync(requestConfiguration =>
            {
                requestConfiguration.QueryParameters.Select = new[] 
                { 
                    "displayName", 
                    "mail", 
                    "userPrincipalName",
                    "jobTitle",
                    "officeLocation"
                };
            });

            if (user != null)
            {
                return new UserInfo
                {
                    DisplayName = user.DisplayName ?? "Unknown User",
                    Email = user.Mail ?? user.UserPrincipalName ?? "",
                    JobTitle = user.JobTitle ?? "",
                    OfficeLocation = user.OfficeLocation ?? ""
                };
            }
        }
        catch (ServiceException ex)
        {
            System.Diagnostics.Debug.WriteLine($"Graph API error getting user: {ex.ResponseStatusCode} - {ex.Message}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting current user: {ex.Message}");
        }

        return null;
    }

    public async Task<List<string>> GetCalendarsAsync()
    {
        var calendarNames = new List<string>();

        try
        {
            if (_graphServiceClient == null)
            {
                throw new InvalidOperationException("Calendar service not initialized");
            }

            var calendars = await _graphServiceClient.Me.Calendars.GetAsync(requestConfiguration =>
            {
                requestConfiguration.QueryParameters.Select = new[] { "name", "id", "canEdit" };
            });

            if (calendars?.Value != null)
            {
                foreach (var calendar in calendars.Value)
                {
                    if (!string.IsNullOrEmpty(calendar.Name))
                    {
                        calendarNames.Add(calendar.Name);
                    }
                }
            }
        }
        catch (ServiceException ex)
        {
            System.Diagnostics.Debug.WriteLine($"Graph API error getting calendars: {ex.ResponseStatusCode} - {ex.Message}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting calendars: {ex.Message}");
        }

        return calendarNames;
    }

    private CalendarEventInfo ConvertToCalendarEventInfo(Event eventItem)
    {
        var eventInfo = new CalendarEventInfo
        {
            Subject = eventItem.Subject ?? "No Subject",
            IsAllDay = eventItem.IsAllDay ?? false,
            Location = eventItem.Location?.DisplayName ?? "",
            BodyPreview = eventItem.BodyPreview ?? "",
            ShowAs = eventItem.ShowAs?.ToString() ?? "Busy",
            Sensitivity = eventItem.Sensitivity?.ToString() ?? "Normal"
        };

        // Parse start time
        if (eventItem.Start?.DateTime != null)
        {
            if (DateTime.TryParse(eventItem.Start.DateTime, out var startTime))
            {
                eventInfo.StartTime = startTime;
            }
        }

        // Parse end time
        if (eventItem.End?.DateTime != null)
        {
            if (DateTime.TryParse(eventItem.End.DateTime, out var endTime))
            {
                eventInfo.EndTime = endTime;
            }
        }

        // Get organizer info
        if (eventItem.Organizer?.EmailAddress != null)
        {
            eventInfo.OrganizerName = eventItem.Organizer.EmailAddress.Name ?? "";
            eventInfo.OrganizerEmail = eventItem.Organizer.EmailAddress.Address ?? "";
        }

        // Get attendees count
        if (eventItem.Attendees != null)
        {
            eventInfo.AttendeesCount = eventItem.Attendees.Count();
        }

        return eventInfo;
    }
}

public class CalendarEventInfo
{
    public string Subject { get; set; } = "";
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public bool IsAllDay { get; set; }
    public string Location { get; set; } = "";
    public string BodyPreview { get; set; } = "";
    public string ShowAs { get; set; } = "";
    public string Sensitivity { get; set; } = "";
    public string OrganizerName { get; set; } = "";
    public string OrganizerEmail { get; set; } = "";
    public int AttendeesCount { get; set; }

    public string GetTimeDisplay()
    {
        if (IsAllDay)
        {
            return "All Day";
        }

        return $"{StartTime:h:mm tt} - {EndTime:h:mm tt}";
    }

    public string GetDurationDisplay()
    {
        if (IsAllDay)
        {
            return "All Day";
        }

        var duration = EndTime - StartTime;
        if (duration.TotalDays >= 1)
        {
            return $"{duration.Days}d {duration.Hours}h {duration.Minutes}m";
        }
        else if (duration.TotalHours >= 1)
        {
            return $"{duration.Hours}h {duration.Minutes}m";
        }
        else
        {
            return $"{duration.Minutes}m";
        }
    }

    public string GetStatusColor()
    {
        return ShowAs.ToLower() switch
        {
            "free" => "#28A745",      // Green
            "tentative" => "#FFC107",  // Yellow
            "busy" => "#DC3545",       // Red
            "oof" => "#6F42C1",        // Purple (Out of Office)
            "workingelsewhere" => "#17A2B8", // Cyan
            _ => "#6C757D"             // Gray (Unknown)
        };
    }
}

public class UserInfo
{
    public string DisplayName { get; set; } = "";
    public string Email { get; set; } = "";
    public string JobTitle { get; set; } = "";
    public string OfficeLocation { get; set; } = "";
}