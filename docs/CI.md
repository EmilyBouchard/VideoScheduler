# CI/CD and Testing

This document describes the Continuous Integration/Continuous Deployment (CI/CD) setup for VideoScheduler and how to run tests locally.

## Overview

The project uses GitHub Actions for CI/CD with two main workflows:

1. **Build and Test** (`build.yml`) - Runs on every push to `main` and all pull requests
2. **Release** (`release.yml`) - Creates release artifacts when a version tag is pushed

## Running Tests Locally

### Prerequisites

- .NET 9.0 SDK or later
- Windows OS (for WPF project compilation)

### Build the Solution

```bash
dotnet restore VideoScheduler.sln
dotnet build VideoScheduler.sln --configuration Debug
```

### Run All Tests

```bash
dotnet test VideoScheduler.sln --verbosity normal
```

### Run Tests with Coverage

```bash
dotnet test VideoScheduler.sln --collect:"XPlat Code Coverage"
```

Coverage reports will be generated in the `TestResults` directory.

### Run Specific Test Projects

```bash
# Domain tests only
dotnet test VideoScheduler.Domain.Tests/VideoScheduler.Domain.Tests.csproj

# Application tests only
dotnet test VideoScheduler.Application.Tests/VideoScheduler.Application.Tests.csproj
```

## Test Structure

### Test Projects

- **VideoScheduler.Domain.Tests** - Unit tests for domain entities and value objects
  - No external dependencies
  - Tests pure domain logic
  - Example: `VideoAssetTests.cs`

- **VideoScheduler.Application.Tests** - Unit tests for application services and use cases
  - Uses NSubstitute for mocking
  - Tests application logic with mocked dependencies
  - Example: `VideoMetadataServiceTests.cs`

### Testing Libraries

- **xUnit** - Test framework
- **FluentAssertions** - Assertion library for readable test assertions
- **NSubstitute** - Mocking library for creating test doubles
- **coverlet.collector** - Code coverage collection

### Test Helpers

The `TestServiceProvider` class in `VideoScheduler.Application.Tests/Helpers/` provides a DI-based composition pattern for tests:

```csharp
using var provider = TestServiceProvider.Create(services =>
{
    services.AddSubstitute<IVideoMetadataService>();
    // Add more services as needed
});

var service = provider.GetRequiredService<IVideoMetadataService>();
```

## CI/CD Workflows

### Build and Test Workflow

**Trigger:** Push to `main` or any pull request targeting `main`

**Steps:**
1. Checkout code
2. Setup .NET 9.0.x SDK
3. Cache NuGet packages (for faster builds)
4. Restore dependencies
5. Build in Release configuration
6. Run all tests with code coverage
7. Upload coverage reports as artifacts (retained for 30 days)
8. Upload test results as artifacts (retained for 30 days)

**Failure Conditions:**
- Build errors
- Test failures
- Any step returning a non-zero exit code

### Release Workflow

**Trigger:** Push of a tag matching `v*` (e.g., `v1.0.0`, `v2.1.3`)

**Steps:**
1. Checkout code
2. Setup .NET 9.0.x SDK
3. Cache NuGet packages
4. Restore dependencies
5. Build in Release configuration
6. Run all tests (release fails if tests fail)
7. Publish the WPF application
8. Create a ZIP archive of the published application
9. Upload artifact to workflow run (retained for 90 days)
10. Create a GitHub Release with the artifact attached

**Creating a Release:**

```bash
# Tag the commit
git tag -a v1.0.0 -m "Release version 1.0.0"

# Push the tag
git push origin v1.0.0
```

The workflow will automatically:
- Build and test the application
- Create a release package
- Attach the package to a GitHub Release
- Generate release notes from commits

## Test Artifacts

### Local Development

Test artifacts are excluded from version control via `.gitignore`:
- `bin/` - Build outputs
- `obj/` - Intermediate build files
- `TestResults/` - Test execution results
- `coverage/` - Code coverage reports

### CI/CD

On GitHub Actions:
- Coverage reports are available as downloadable artifacts for 30 days
- Release artifacts are available for 90 days
- All logs are available in the workflow run details

## Best Practices

1. **Run tests before committing** - Always run `dotnet test` locally before pushing
2. **Keep tests fast** - Domain and application tests should complete in seconds
3. **Mock external dependencies** - Use NSubstitute to mock I/O operations
4. **Follow AAA pattern** - Arrange, Act, Assert in all tests
5. **Use descriptive test names** - Test method names should describe what they test
6. **Keep test code clean** - Apply same quality standards as production code

## Troubleshooting

### Tests fail locally but pass in CI (or vice versa)

- Check .NET SDK version: `dotnet --version`
- Ensure you're building in the same configuration (Debug vs Release)
- Verify all dependencies are restored: `dotnet restore`

### Build fails on EnableWindowsTargeting

The WPF project requires `EnableWindowsTargeting` to build on non-Windows systems in CI. This is already configured in the project file.

### Coverage reports not generated

Ensure you're using the `--collect:"XPlat Code Coverage"` flag with `dotnet test`.

## Future Enhancements

Potential improvements to the CI/CD pipeline:

- Code coverage thresholds and reporting
- Performance testing
- Integration tests with file system operations
- Automated UI tests (when feasible)
- Multi-environment deployments
- Automated version bumping
