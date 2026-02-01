# Custom AutoFixture Customization

Use `ICustomization` to centralize AutoFixture configuration across multiple test classes.

## When to Use

Create custom customizations when:
- Multiple test classes need the same specimen builders
- You want to set default values for configuration objects
- Centralizing test infrastructure setup
- Grouping related builders together

## Basic Pattern

```csharp
using AutoFixture;

public class MyDomainCustomization : ICustomization
{
    public void Customize(IFixture fixture)
    {
        // Add specimen builders
        fixture.Customizations.Add(new MyObjectBuilder());
        fixture.Customizations.Add(new AnotherObjectBuilder());

        // Set default values
        fixture.Inject(new MyOptions { Setting = "test" });

        // Configure specific behaviors
        fixture.Register<IService>(() =>
            fixture.Freeze<Mock<IService>>().Object);
    }
}
```

## Complete Example

Centralized customization for all backend tests:

```csharp
using AutoFixture;

/// <summary>
/// Centralized AutoFixture customization for all backend tests.
/// Configures specimen builders, default values, and mock behaviors.
/// </summary>
public class BackendDomainCustomization : ICustomization
{
    public void Customize(IFixture fixture)
    {
        // Add custom specimen builders for complex domain objects
        fixture.Customizations.Add(new EmbeddingVectorBuilder());
        fixture.Customizations.Add(new DocumentMetadataBuilder());
        fixture.Customizations.Add(new DocumentChunkBuilder());
        fixture.Customizations.Add(new SearchResultBuilder());
        fixture.Customizations.Add(new AskQuestionRequestBuilder());
        fixture.Customizations.Add(new AskQuestionResponseBuilder());

        // Configure default test values
        fixture.Inject(CreateDefaultApplicationOptions());
        fixture.Inject(CreateDefaultBackendOptions());
    }

    private static ApplicationOptions CreateDefaultApplicationOptions()
    {
        return ApplicationOptions.Create(
            maxSearchResults: 10,
            systemPrompt: "Test system prompt for unit tests"
        );
    }

    private static BackendOptions CreateDefaultBackendOptions()
    {
        return new BackendOptions
        {
            EmbeddingsFilePath = "Data/test-embeddings.json",
            LlmProvider = LlmProvider.OpenAI,
            OpenAIApiKey = "test-openai-key-12345",
            OpenAIEmbeddingModel = "text-embedding-3-small",
            OpenAIChatModel = "gpt-4o-mini",
            MaxSearchResults = 10,
            MemoryCollectionName = "test-collection",
            AllowedOrigins = []
        };
    }
}
```

## Using the Customization

Apply the customization in test class setup:

```csharp
[TestFixture]
public class MyServiceTests
{
    private IFixture _fixture;
    private Mock<IDependency> _dependencyMock;
    private MyService _sut;

    [SetUp]
    public void SetUp()
    {
        // Apply AutoMoq + custom domain customization
        _fixture = new Fixture()
            .Customize(new AutoMoqCustomization())
            .Customize(new BackendDomainCustomization());

        // Freeze dependencies
        _dependencyMock = _fixture.Freeze<Mock<IDependency>>();

        // Resolve SUT
        _sut = _fixture.Create<MyService>();
    }

    [Test]
    public void SomeTest()
    {
        // Domain objects are automatically created with builders
        var chunk = _fixture.Create<DocumentChunk>();
        var results = _fixture.CreateMany<SearchResult>(5);

        // Configuration objects have realistic defaults
        var options = _fixture.Create<ApplicationOptions>();
        Assert.That(options.MaxSearchResults, Is.EqualTo(10));
    }
}
```

## Layer-Specific Customizations

Create customizations for each layer:

```csharp
// Domain layer customization
public class DomainCustomization : ICustomization
{
    public void Customize(IFixture fixture)
    {
        fixture.Customizations.Add(new EntityBuilder());
        fixture.Customizations.Add(new ValueObjectBuilder());
    }
}

// Infrastructure layer customization
public class InfrastructureCustomization : ICustomization
{
    public void Customize(IFixture fixture)
    {
        fixture.Customize(new DomainCustomization());  // Include domain
        fixture.Customizations.Add(new RepositoryBuilder());
    }
}

// Application layer customization
public class ApplicationCustomization : ICustomization
{
    public void Customize(IFixture fixture)
    {
        fixture.Customize(new DomainCustomization());  // Include domain
        fixture.Customizations.Add(new CommandBuilder());
        fixture.Customizations.Add(new QueryBuilder());
    }
}
```

## Advanced: Conditional Customization

Customize based on context:

```csharp
public class ConditionalCustomization : ICustomization
{
    private readonly bool _useRealImplementation;

    public ConditionalCustomization(bool useRealImplementation = false)
    {
        _useRealImplementation = useRealImplementation;
    }

    public void Customize(IFixture fixture)
    {
        if (_useRealImplementation)
        {
            fixture.Register<IService>(() => new RealService());
        }
        else
        {
            fixture.Register<IService>(() =>
                fixture.Freeze<Mock<IService>>().Object);
        }
    }
}
```

## Organizing Customizations

Structure customizations by domain or layer:

```
MyProject.Tests/
├── TestInfrastructure/
│   ├── DomainCustomization.cs
│   ├── ApplicationCustomization.cs
│   └── InfrastructureCustomization.cs
│   └── Builders/
│       ├── EntityBuilder.cs
│       ├── ValueObjectBuilder.cs
│       └── DtoBuilder.cs
```

## Best Practices

**✅ Do:**
- Create one customization per layer/module
- Chain customizations (include lower layers)
- Provide realistic default values
- Document what the customization configures

**❌ Don't:**
- Put test logic in customizations (only setup)
- Create global shared state
- Modify fixture after customization applied
- Override AutoMoqCustomization behavior

## Inheritance Pattern

For test base classes:

```csharp
/// <summary>
/// Base class for all backend service tests.
/// Provides pre-configured fixture with domain customizations.
/// </summary>
public abstract class BackendServiceTestBase
{
    protected IFixture Fixture { get; private set; }

    [SetUp]
    public virtual void BaseSetUp()
    {
        Fixture = new Fixture()
            .Customize(new AutoMoqCustomization())
            .Customize(new BackendDomainCustomization());
    }
}

[TestFixture]
public class MyServiceTests : BackendServiceTestBase
{
    private Mock<IDependency> _dependencyMock;
    private MyService _sut;

    [SetUp]
    public void SetUp()
    {
        // Fixture already configured by base class
        _dependencyMock = Fixture.Freeze<Mock<IDependency>>();
        _sut = Fixture.Create<MyService>();
    }
}
```

## Testing Customizations

Verify customizations work correctly:

```csharp
[Test]
public void Customization_RegistersAllBuilders()
{
    // Arrange
    var fixture = new Fixture()
        .Customize(new BackendDomainCustomization());

    // Act & Assert - should not throw
    var chunk = fixture.Create<DocumentChunk>();
    var vector = fixture.Create<EmbeddingVector>();
    var options = fixture.Create<ApplicationOptions>();

    Assert.That(chunk, Is.Not.Null);
    Assert.That(vector, Is.Not.Null);
    Assert.That(options.MaxSearchResults, Is.EqualTo(10));
}
```

## When Not to Use

Don't create customizations for:
- One-off test scenarios (configure fixture directly in test)
- Test-specific mocks (use Freeze in individual tests)
- Simple POCOs that AutoFixture handles automatically
