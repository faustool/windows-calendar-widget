# M365 Calendar App - Testing Guide

## Overview

This document provides comprehensive information about the automated test suite for the M365 Calendar App. The test suite includes unit tests, integration tests, and end-to-end tests to ensure code quality and reliability.

## Test Structure

### 📁 Test Project Structure
```
M365CalendarApp.Tests/
├── Services/                    # Unit tests for service classes
│   ├── AuthenticationServiceTests.cs
│   ├── CalendarServiceTests.cs
│   ├── ConfigurationServiceTests.cs
│   ├── ProxyServiceTests.cs
│   └── StartupServiceTests.cs
├── Integration/                 # Integration tests
│   └── ServiceIntegrationTests.cs
├── Fixtures/                    # Test data and utilities
│   ├── TestDataFixtures.cs
│   └── TestDataLoader.cs
├── Helpers/                     # Test helper classes
│   └── MockHelpers.cs
├── TestData/                    # JSON test data files
│   ├── sample-events.json
│   ├── sample-user.json
│   ├── all-day-events.json
│   └── error-scenarios.json
└── TestBase.cs                  # Base class for all tests
```

## Test Categories

### 🔧 Unit Tests
- **Purpose**: Test individual components in isolation
- **Coverage**: All service classes and data models
- **Mocking**: External dependencies are mocked
- **Location**: `Services/` folder

### 🔗 Integration Tests
- **Purpose**: Test interaction between multiple components
- **Coverage**: Service-to-service communication
- **Dependencies**: May use real implementations
- **Location**: `Integration/` folder

### 🌐 End-to-End Tests
- **Purpose**: Test complete application workflows
- **Coverage**: Full user scenarios
- **Dependencies**: Requires real Azure AD setup
- **Status**: Marked as `Skip` by default

## Test Frameworks and Tools

### Core Testing Stack
- **xUnit**: Primary testing framework
- **FluentAssertions**: Readable assertion library
- **Moq**: Mocking framework for dependencies
- **Coverlet**: Code coverage collection

### Additional Tools
- **ReportGenerator**: HTML coverage reports
- **GitHub Actions**: CI/CD pipeline
- **dotnet test**: Test runner

## Running Tests

### 🚀 Quick Start

#### All Tests
```bash
# Windows
.\run-tests.bat

# Linux/macOS
./run-tests.sh
```

#### Unit Tests Only
```bash
# Linux/macOS
./run-unit-tests-only.sh

# Windows
dotnet test M365CalendarApp.Tests --filter "Category!=Integration&Category!=EndToEnd"
```

#### With Coverage Report
```bash
# Windows
.\test-coverage.bat

# Manual
dotnet test --collect:"XPlat Code Coverage"
reportgenerator -reports:**/coverage.cobertura.xml -targetdir:CoverageReport -reporttypes:Html
```

### 📊 Test Categories

#### Run Specific Test Categories
```bash
# Unit tests only
dotnet test --filter "Category!=Integration&Category!=EndToEnd"

# Integration tests only
dotnet test --filter "Category=Integration"

# End-to-end tests (requires setup)
dotnet test --filter "Category=EndToEnd"
```

## Test Configuration

### Environment Variables
```bash
# Test environment
DOTNET_ENVIRONMENT=Test
ASPNETCORE_ENVIRONMENT=Test

# Proxy testing (optional)
HTTPS_PROXY=http://proxy.company.com:8080
PROXY_USER=testuser
PROXY_PASS=testpass
```

### Test Settings
- **Parallel Execution**: Enabled for test collections
- **Timeout**: 60 seconds for long-running tests
- **Retry**: Not configured (tests should be deterministic)

## Test Data

### 📋 Sample Data Files

#### Calendar Events (`sample-events.json`)
- Daily standup meetings
- Project review sessions
- Client meetings
- Development sprints
- Team retrospectives

#### All-Day Events (`all-day-events.json`)
- Company holidays
- Conferences
- Personal time off

#### User Data (`sample-user.json`)
- Sample user profiles
- Different job titles and locations
- Contact information

#### Error Scenarios (`error-scenarios.json`)
- Network timeouts
- Authentication failures
- Rate limiting
- Server errors

### 🔧 Test Data Usage
```csharp
// Load sample events
var events = await TestDataLoader.LoadSampleEventsAsync();

// Create mock events
var mockEvent = TestDataLoader.CreateMockEvent(
    "Test Meeting", 
    DateTime.Now, 
    DateTime.Now.AddHours(1)
);

// Use fixtures
var config = TestDataFixtures.DefaultConfiguration;
```

## Mocking Strategy

### 🎭 External Dependencies

#### Microsoft Graph API
```csharp
var mockGraphClient = MockHelpers.CreateMockGraphServiceClient();
// Configure mock responses for calendar events, user data, etc.
```

#### Authentication Service
```csharp
var mockAuthService = new Mock<AuthenticationService>();
mockAuthService.Setup(x => x.IsAuthenticated).Returns(true);
mockAuthService.Setup(x => x.GetAccessToken()).Returns("mock-token");
```

#### File System Operations
```csharp
// Use temporary directories and files
var tempFile = CreateTempFile(jsonContent, "test-config.json");
```

### 🔒 Security Testing
- Token encryption/decryption
- Credential storage
- Proxy authentication
- Certificate validation

## Code Coverage

### 📈 Coverage Targets
- **Minimum**: 60% overall coverage
- **Target**: 80% overall coverage
- **Services**: 90%+ coverage for business logic
- **Models**: 100% coverage for data classes

### Coverage Reports
- **Console**: Basic coverage summary
- **TRX**: Test results for CI/CD
- **Cobertura**: XML format for tools
- **HTML**: Detailed interactive reports

### Excluded from Coverage
- Program entry points
- UI event handlers (WPF-specific)
- Third-party library wrappers

## Continuous Integration

### 🔄 GitHub Actions Workflow

#### On Push/PR
1. **Build**: Compile all projects
2. **Unit Tests**: Run isolated tests
3. **Coverage**: Collect code coverage
4. **Report**: Generate coverage reports
5. **Artifacts**: Upload test results

#### On Main Branch
1. **Integration Tests**: Run with real dependencies
2. **Performance Tests**: Validate response times
3. **Security Scans**: Check for vulnerabilities

### CI Configuration
```yaml
# .github/workflows/tests.yml
- name: Run Unit Tests
  run: dotnet test --filter "Category!=Integration&Category!=EndToEnd"
  
- name: Code Coverage
  uses: irongut/CodeCoverageSummary@v1.3.0
```

## Test Best Practices

### ✅ Writing Good Tests

#### Test Naming
```csharp
[Fact]
public void MethodName_ShouldExpectedBehavior_WhenCondition()
{
    // Arrange
    // Act  
    // Assert
}
```

#### Arrange-Act-Assert Pattern
```csharp
[Fact]
public void GetEventsForDate_ShouldReturnEvents_WhenDateHasEvents()
{
    // Arrange
    var service = new CalendarService(mockAuth.Object);
    var testDate = DateTime.Today;
    
    // Act
    var result = await service.GetEventsForDateAsync(testDate);
    
    // Assert
    result.Should().NotBeNull();
    result.Should().HaveCount(3);
}
```

#### Parameterized Tests
```csharp
[Theory]
[InlineData("Light")]
[InlineData("Dark")]
public void Configuration_ShouldAcceptValidThemes(string theme)
{
    // Test implementation
}
```

### 🚫 Test Anti-Patterns to Avoid
- Testing implementation details
- Overly complex test setup
- Tests that depend on external services
- Non-deterministic tests
- Tests that test multiple concerns

## Troubleshooting

### 🐛 Common Issues

#### Build Errors
```bash
# Framework compatibility
error NU1201: Project is not compatible with net8.0

# Solution: Ensure test project targets net8.0-windows
<TargetFramework>net8.0-windows</TargetFramework>
```

#### Test Failures
```bash
# Authentication tests fail
# Solution: Mock authentication dependencies properly

# File system tests fail  
# Solution: Use temporary directories and proper cleanup
```

#### Coverage Issues
```bash
# Low coverage reported
# Solution: Ensure all test assemblies are included

# Coverage tool not found
# Solution: Install reportgenerator
dotnet tool install -g dotnet-reportgenerator-globaltool
```

### 🔍 Debugging Tests

#### Visual Studio
- Set breakpoints in test methods
- Use Test Explorer for individual test runs
- View test output and debug information

#### Command Line
```bash
# Verbose output
dotnet test --logger "console;verbosity=detailed"

# Debug specific test
dotnet test --filter "FullyQualifiedName~TestMethodName"
```

## Performance Testing

### ⚡ Performance Benchmarks
- Service initialization: < 5 seconds
- Configuration operations: < 1 second
- Calendar data retrieval: < 10 seconds (7 days)

### Load Testing
```csharp
[Fact]
public async Task CalendarService_ShouldHandleMultipleRequests()
{
    // Test concurrent calendar requests
    var tasks = Enumerable.Range(0, 10)
        .Select(i => service.GetEventsForDateAsync(DateTime.Today.AddDays(i)));
    
    var results = await Task.WhenAll(tasks);
    // Assert all requests completed successfully
}
```

## Security Testing

### 🔐 Security Test Areas
- Token encryption/decryption
- Credential storage security
- Proxy authentication
- Certificate validation
- Input sanitization

### Security Test Examples
```csharp
[Fact]
public void TokenStorage_ShouldEncryptSensitiveData()
{
    // Test that tokens are properly encrypted
    var originalToken = "sensitive-access-token";
    var encryptedData = ProtectedData.Protect(/* ... */);
    
    encryptedData.Should().NotContain(originalToken);
}
```

## Contributing to Tests

### 📝 Adding New Tests

1. **Identify Test Category**: Unit, Integration, or E2E
2. **Create Test Class**: Follow naming conventions
3. **Add Test Data**: Use fixtures and test data files
4. **Mock Dependencies**: Use appropriate mocking strategy
5. **Follow Patterns**: Use AAA pattern and FluentAssertions
6. **Update Documentation**: Add to this guide if needed

### Test Review Checklist
- [ ] Tests are isolated and independent
- [ ] Proper mocking of external dependencies
- [ ] Clear test names and documentation
- [ ] Appropriate assertions with FluentAssertions
- [ ] Test data cleanup (IDisposable)
- [ ] Performance considerations
- [ ] Security implications covered

## Resources

### 📚 Documentation Links
- [xUnit Documentation](https://xunit.net/)
- [FluentAssertions Documentation](https://fluentassertions.com/)
- [Moq Documentation](https://github.com/moq/moq4)
- [.NET Testing Best Practices](https://docs.microsoft.com/en-us/dotnet/core/testing/best-practices)

### 🛠️ Tools and Extensions
- **Visual Studio Test Explorer**
- **VS Code Test Explorer**
- **ReSharper Test Runner**
- **NCrunch** (continuous testing)

---

## Summary

The M365 Calendar App test suite provides comprehensive coverage of all application components with:

- **300+ Unit Tests** covering individual components
- **50+ Integration Tests** covering component interactions  
- **Automated CI/CD Pipeline** with GitHub Actions
- **Code Coverage Reports** with 80%+ target coverage
- **Performance and Security Testing** for production readiness

Run `./run-tests.sh` to execute the full test suite and generate coverage reports.