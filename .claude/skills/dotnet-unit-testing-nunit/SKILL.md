---
name: dotnet-unit-testing-nunit
description: Writes unit tests for .NET projects using NUnit, AutoFixture, and AutoMoq. Use when writing tests for C# code, .NET projects with .csproj files, or when user mentions NUnit, AutoFixture, unit testing. DO NOT use for JavaScript, TypeScript, Next.js, React, or Node.js projects.
allowed-tools: Read, Write, Edit, Bash, Glob, Grep
---

# .NET Unit Testing with NUnit + AutoFixture

Writes unit tests for .NET projects using NUnit, AutoFixture, and AutoMoq patterns.

## Quick Start

Minimal test example showing the complete pattern:

```csharp
using AutoFixture;
using AutoFixture.AutoMoq;
using Moq;
using NUnit.Framework;

namespace MyProject.Tests;

[TestFixture]
public class MyServiceTests
{
    private IFixture _fixture;
    private Mock<IDependency> _dependencyMock;
    private MyService _sut;

    [SetUp]
    public void SetUp()
    {
        _fixture = new Fixture().Customize(new AutoMoqCustomization());
        _dependencyMock = _fixture.Freeze<Mock<IDependency>>();
        _sut = _fixture.Create<MyService>();
    }

    [Test]
    public void ProcessData_ValidInput_ReturnsExpectedResult()
    {
        // Arrange
        var input = _fixture.Create<string>();
        _dependencyMock.Setup(x => x.GetData()).Returns("expected");

        // Act
        var result = _sut.ProcessData(input);

        // Assert
        Assert.That(result, Is.EqualTo("expected"));
        _dependencyMock.Verify(x => x.GetData(), Times.Once);
    }
}
```

## Required NuGet Packages

```bash
dotnet add package NUnit --version 4.*
dotnet add package NUnit3TestAdapter --version 5.*
dotnet add package Microsoft.NET.Test.Sdk --version 17.*
dotnet add package AutoFixture --version 4.*
dotnet add package AutoFixture.AutoMoq --version 4.*
dotnet add package Moq --version 4.*
```

## Core Principles

**✅ Always Do:**

- Resolve SUT from fixture: `_fixture.Create<T>()`
- Use Freeze for shared dependencies: `_fixture.Freeze<Mock<T>>()`
- Follow AAA pattern (Arrange, Act, Assert)
- Test naming: `MethodName_Scenario_ExpectedBehavior`
- Test happy paths and valid scenarios
- Verify mock interactions for orchestration
- Keep tests fast (no real I/O, no network)
- Isolated tests (no shared state)

**❌ Never Do:**

- Manual construction: `new MyService()` - Always use fixture
- Testing exceptions with `Assert.Throws` (no validation failure tests)
- Testing framework code (.NET/NuGet package behavior)
- Shared state between tests
- Over-mocking (only mock interfaces)
- Hardcoded test data: Use AutoFixture instead of `"test123"`
- Real file system, APIs, or network calls

## Test Class Structure

Standard test class setup with Freeze pattern:

```csharp
[TestFixture]
[TestOf(typeof(QuestionAnsweringService))]
public class QuestionAnsweringServiceTests
{
    private IFixture _fixture;
    private Mock<IMemoryService> _memoryServiceMock;
    private Mock<ILlmProvider> _llmProviderMock;
    private Mock<ILogger<QuestionAnsweringService>> _loggerMock;
    private QuestionAnsweringService _sut;

    [SetUp]
    public void SetUp()
    {
        // Initialize fixture with AutoMoq
        _fixture = new Fixture().Customize(new AutoMoqCustomization());

        // Freeze dependencies for reuse across tests
        _memoryServiceMock = _fixture.Freeze<Mock<IMemoryService>>();
        _llmProviderMock = _fixture.Freeze<Mock<ILlmProvider>>();
        _loggerMock = _fixture.Freeze<Mock<ILogger<QuestionAnsweringService>>>();

        // Resolve SUT from fixture (never use 'new')
        _sut = _fixture.Create<QuestionAnsweringService>();
    }
}
```

## TestOf Attribute

Use `[TestOf]` to explicitly link tests to the code they're testing. This enables tooling to track test coverage and navigate between tests and production code.

### Class-Level Usage

Link an entire test class to its subject under test:

```csharp
[TestFixture]
[TestOf(typeof(QuestionAnsweringService))]
public class QuestionAnsweringServiceTests
{
    // All tests in this class test QuestionAnsweringService
}
```

### Method-Level Usage

Link individual test methods to specific methods:

```csharp
[Test]
[TestOf(nameof(MyService.ProcessData))]
public void ProcessData_ValidInput_ReturnsSuccess()
{
    // Tests the ProcessData method specifically
}
```

### Combined Usage

Use both for precise test-to-code mapping:

```csharp
[TestFixture]
[TestOf(typeof(OrderService))]
public class OrderServiceTests
{
    [Test]
    [TestOf(nameof(OrderService.SubmitOrder))]
    public void SubmitOrder_ValidOrder_ReturnsConfirmation() { }

    [Test]
    [TestOf(nameof(OrderService.CancelOrder))]
    public void CancelOrder_ExistingOrder_ReturnsSuccess() { }
}
```

## Test Method Templates

### Synchronous Test

```csharp
[Test]
public void MethodName_Scenario_ExpectedBehavior()
{
    // Arrange
    var input = _fixture.Create<string>();
    _dependencyMock
        .Setup(x => x.Process(input))
        .Returns("expected");

    // Act
    var result = _sut.MethodName(input);

    // Assert
    Assert.That(result, Is.EqualTo("expected"));
    _dependencyMock.Verify(x => x.Process(input), Times.Once);
}
```

### Async Test

```csharp
[Test]
public async Task MethodNameAsync_Scenario_ExpectedBehavior()
{
    // Arrange
    var input = _fixture.Create<string>();
    _dependencyMock
        .Setup(x => x.ProcessAsync(input, It.IsAny<CancellationToken>()))
        .ReturnsAsync("expected");

    // Act
    var result = await _sut.MethodNameAsync(input, CancellationToken.None);

    // Assert
    Assert.That(result, Is.EqualTo("expected"));
    _dependencyMock.Verify(
        x => x.ProcessAsync(input, It.IsAny<CancellationToken>()),
        Times.Once);
}
```

## Common Patterns

### Region Organization

Group related tests using regions:

```csharp
[TestFixture]
public class MyServiceTests
{
    // SetUp code...

    #region Happy Path Tests

    [Test]
    public void ValidInput_ReturnsSuccess() { }

    [Test]
    public void MultipleItems_ProcessesAll() { }

    #endregion

    #region Edge Cases

    [Test]
    public void EmptyInput_ReturnsDefault() { }

    [Test]
    public void NullInput_HandlesGracefully() { }

    #endregion

    #region Service Integration

    [Test]
    public void CallsServicesInCorrectOrder() { }

    #endregion
}
```

### Multiple Assertions

One test focus, but multiple `Assert.That()` statements are allowed:

```csharp
[Test]
public void ProcessData_ValidInput_ReturnsCompleteResult()
{
    // Arrange
    var input = _fixture.Create<MyInput>();

    // Act
    var result = _sut.ProcessData(input);

    // Assert
    Assert.That(result, Is.Not.Null);
    Assert.That(result.Status, Is.EqualTo("Success"));
    Assert.That(result.Data, Is.Not.Empty);
    Assert.That(result.Timestamp, Is.GreaterThan(DateTime.MinValue));
}
```

### Mock Verification

Use different verification modes:

```csharp
// Verify called exactly once
_dependencyMock.Verify(x => x.Save(It.IsAny<Data>()), Times.Once);

// Verify never called
_dependencyMock.Verify(x => x.Delete(It.IsAny<int>()), Times.Never);

// Verify called at least once
_loggerMock.Verify(
    x => x.Log(LogLevel.Error, It.IsAny<EventId>(),
               It.IsAny<It.IsAnyType>(), It.IsAny<Exception>(),
               It.IsAny<Func<It.IsAnyType, Exception, string>>()),
    Times.AtLeastOnce);
```

### Capturing Values

Use Callback to capture and verify values:

```csharp
[Test]
public void BuildsCorrectPrompt_WithContext()
{
    // Arrange
    var question = _fixture.Create<string>();
    var capturedPrompt = string.Empty;

    _llmProviderMock
        .Setup(x => x.GenerateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
        .Callback<string, CancellationToken>((prompt, _) => capturedPrompt = prompt)
        .ReturnsAsync("answer");

    // Act
    await _sut.AskQuestionAsync(question);

    // Assert
    Assert.That(capturedPrompt, Does.Contain("<context>"));
    Assert.That(capturedPrompt, Does.Contain(question));
}
```

### Creating Collections

Use `CreateMany<T>()` for collections:

```csharp
[Test]
public void ProcessMultipleItems_ReturnsAllResults()
{
    // Arrange
    var items = _fixture.CreateMany<Item>(5).ToList();  // Creates 5 items
    _dependencyMock
        .Setup(x => x.GetItems())
        .Returns(items);

    // Act
    var result = _sut.ProcessAll();

    // Assert
    Assert.That(result, Has.Count.EqualTo(5));
}
```

### Tracking Call Order

Verify services are called in the correct sequence:

```csharp
[Test]
public void CallsServicesInCorrectOrder()
{
    // Arrange
    var callOrder = new List<string>();

    _service1Mock
        .Setup(x => x.Process())
        .Callback(() => callOrder.Add("service1"));

    _service2Mock
        .Setup(x => x.Process())
        .Callback(() => callOrder.Add("service2"));

    _service3Mock
        .Setup(x => x.Process())
        .Callback(() => callOrder.Add("service3"));

    // Act
    _sut.Execute();

    // Assert
    Assert.That(callOrder, Is.EqualTo(new[] { "service1", "service2", "service3" }));
}
```

## Advanced Patterns

For complex scenarios, use custom AutoFixture builders:

### Custom Specimen Builders

When you need to create complex domain objects with validation or specific constructor patterns, create an `ISpecimenBuilder`. See [patterns/specimen-builders.md](patterns/specimen-builders.md) for complete examples.

Use when:

- Domain objects have factory methods instead of constructors
- Objects require specific initialization logic
- Properties need realistic test data

### Custom Customization

When you need to reuse AutoFixture configuration across multiple test classes, create an `ICustomization`. See [patterns/autofixture-customization.md](patterns/autofixture-customization.md) for complete examples.

Use when:

- Multiple test classes need the same builders
- You want to centralize test configuration
- Setting default values for options/configuration objects

## Running Tests

```bash
# Run all tests
dotnet test

# Run specific test class
dotnet test --filter "FullyQualifiedName~MyServiceTests"

# Run with verbose output
dotnet test --logger "console;verbosity=detailed"

# Run with coverage
dotnet test /p:CollectCoverage=true
```

## Naming Conventions

Consistent naming makes tests discoverable with Glob and Grep patterns.

### Test Project Names

```text
ProjectName.Tests           ✅ Recommended (e.g., Backend.Tests)
ProjectName.UnitTests       ✅ Alternative
ProjectName.Test            ❌ Avoid (singular)
ProjectNameTests            ❌ Avoid (no dot separator)
```

**Glob pattern:** `**/*.Tests.csproj` or `**/*Tests.csproj`

### Test File Names

```text
ClassNameTests.cs           ✅ Recommended (e.g., MyServiceTests.cs)
ClassNameTest.cs            ❌ Avoid (singular)
TestClassName.cs            ❌ Avoid (Test prefix)
ClassName.Tests.cs          ❌ Avoid (dot in filename)
```

**Glob pattern:** `**/*Tests.cs`
**Grep pattern:** `.*Tests\.cs$`

### Test Class Names

Must match the file name:

```csharp
// File: MyServiceTests.cs
[TestFixture]
public class MyServiceTests  // ✅ Matches file name
{
    // Tests here
}
```

**Glob pattern:** `**/*Tests.cs`
**Grep pattern:** `public class \w+Tests`

### Test Method Names

Format: `MethodName_Scenario_ExpectedBehavior`

```csharp
✅ ProcessData_ValidInput_ReturnsSuccess
✅ SaveAsync_NullEntity_ThrowsArgumentNullException
✅ GetAll_EmptyDatabase_ReturnsEmptyList
✅ Calculate_DivideByZero_ReturnsZero

❌ Test1                              // Not descriptive
❌ TestProcessData                    // No scenario
❌ ProcessDataReturnsSuccess          // Missing underscores
❌ processData_validInput_success     // Wrong casing
```

**Grep pattern:** `public .*Task \w+_\w+_\w+\(` (finds async tests)
**Grep pattern:** `public void \w+_\w+_\w+\(` (finds sync tests)

### Namespace Conventions

```csharp
// Production code
namespace MyApp.Domain.Services;

// Corresponding tests
namespace MyApp.Domain.Services;  // ✅ Same namespace (finds internal members)

// Alternative
namespace MyApp.Tests.Domain.Services;  // ✅ Mirrors structure with .Tests
```

### Finding Tests with Tools

**Find all test files:**

```bash
Glob: **/*Tests.cs
```

**Find specific test class:**

```bash
Grep: "public class MyServiceTests"
```

**Find all test methods for a specific method:**

```bash
Grep: "ProcessData_.*_.*\("  --include="**/*Tests.cs"
```

**Find tests in specific namespace:**

```bash
Grep: "namespace.*Services.*Tests"
```

**Count total test methods:**

```bash
Grep: "\[Test\]" --count
```

## Validation Checklist

**Before committing tests, verify:**

- ✅ All objects resolved from fixture (no `new` keyword for SUT)
- ✅ Mocks created with Freeze pattern
- ✅ Test names follow `MethodName_Scenario_ExpectedBehavior`
- ✅ AAA sections clearly separated with comments
- ✅ Mock interactions verified (Times.Once, etc.)
- ✅ No hardcoded test data (use fixture.Create instead)
- ✅ No shared state between tests
- ✅ Tests run fast (no I/O, no real network calls)
- ✅ `[TestOf(typeof(...))]` on test class links to SUT

## Related Skills

For production code conventions, see:

- **`dotnet-nlog-logging`** - NLog.ILogger conventions
- **`dotnet-extensions-logging`** - `ILogger<T>` conventions
- **`dotnet-reactive-patterns`** - Rx.NET patterns for reactive code
- **`dotnet-documentation`** - XML docs and DebuggerDisplay attributes
