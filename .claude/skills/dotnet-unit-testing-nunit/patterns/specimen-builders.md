# Custom Specimen Builders

Use `ISpecimenBuilder` when AutoFixture can't create complex domain objects automatically.

## When to Use

Create custom builders when:

- Domain objects use factory methods instead of public constructors
- Objects require specific validation or business rules
- Properties need realistic values (e.g., 1536-element embedding arrays)
- Constructors have complex parameter requirements

## Basic Pattern

```csharp
using AutoFixture.Kernel;

public class MyDomainObjectBuilder : ISpecimenBuilder
{
    public object Create(object request, ISpecimenContext context)
    {
        if (request is Type type && type == typeof(MyDomainObject))
        {
            // Create the object with required logic
            return MyDomainObject.Create(/* parameters */);
        }

        // Not this type - let AutoFixture handle it
        return new NoSpecimen();
    }
}
```

## Example: Factory Method Pattern

When domain objects use factory methods for validation:

```csharp
using AutoFixture.Kernel;

/// <summary>
/// Builds DocumentChunk using the factory method to ensure validation.
/// </summary>
public class DocumentChunkBuilder : ISpecimenBuilder
{
    public object Create(object request, ISpecimenContext context)
    {
        if (request is Type type && type == typeof(DocumentChunk))
        {
            var id = Guid.NewGuid().ToString();
            var text = $"Sample text content {Guid.NewGuid()}";
            var embedding = Enumerable.Range(0, 1536)
                .Select(_ => (float)(Random.Shared.NextDouble() * 2 - 1))
                .ToArray();
            var source = $"document-{Guid.NewGuid()}.pdf";
            var page = Random.Shared.Next(1, 100);

            // Use factory method instead of constructor
            return DocumentChunk.Create(id, text, embedding, source, page);
        }

        return new NoSpecimen();
    }
}
```

## Example: Value Object with Validation

When objects have private constructors and validation:

```csharp
public class EmbeddingVectorBuilder : ISpecimenBuilder
{
    public object Create(object request, ISpecimenContext context)
    {
        if (request is Type type && type == typeof(EmbeddingVector))
        {
            // Generate realistic embedding vector (1536 dimensions for OpenAI)
            var values = Enumerable.Range(0, 1536)
                .Select(_ => (float)(Random.Shared.NextDouble() * 2 - 1))
                .ToArray();

            return EmbeddingVector.Create(values);
        }

        return new NoSpecimen();
    }
}
```

## Example: Nested Objects with Context

Use `ISpecimenContext` to create nested dependencies:

```csharp
public class SearchResultBuilder : ISpecimenBuilder
{
    public object Create(object request, ISpecimenContext context)
    {
        if (request is Type type && type == typeof(SearchResult))
        {
            // Use context to create nested DocumentChunk
            var chunk = context.Create<DocumentChunk>();
            var score = (float)Random.Shared.NextDouble();

            return new SearchResult(chunk, score);
        }

        return new NoSpecimen();
    }
}
```

## Example: Configuration Objects

Create realistic configuration with valid defaults:

```csharp
public class ApplicationOptionsBuilder : ISpecimenBuilder
{
    public object Create(object request, ISpecimenContext context)
    {
        if (request is Type type && type == typeof(ApplicationOptions))
        {
            return ApplicationOptions.Create(
                maxSearchResults: Random.Shared.Next(5, 20),
                systemPrompt: $"Test system prompt {Guid.NewGuid()}"
            );
        }

        return new NoSpecimen();
    }
}
```

## Registering Builders

Register builders in your fixture setup or customization:

```csharp
[SetUp]
public void SetUp()
{
    _fixture = new Fixture()
        .Customize(new AutoMoqCustomization());

    // Register individual builders
    _fixture.Customizations.Add(new DocumentChunkBuilder());
    _fixture.Customizations.Add(new EmbeddingVectorBuilder());
    _fixture.Customizations.Add(new SearchResultBuilder());

    // Now AutoFixture can create these types
    var chunk = _fixture.Create<DocumentChunk>();
    var results = _fixture.CreateMany<SearchResult>(5);
}
```

Or use a centralized customization (see [autofixture-customization.md](autofixture-customization.md)).

## Best Practices

**✅ Do:**

- Return `new NoSpecimen()` for types you don't handle
- Use `ISpecimenContext` to create nested dependencies
- Generate realistic test data (not just default values)
- Document why the builder is needed

**❌ Don't:**

- Hardcode test data (use random or generated values)
- Create builders for simple POCOs (AutoFixture handles these)
- Throw exceptions from builders
- Modify global state

## Testing Your Builders

Verify builders work correctly:

```csharp
[Test]
public void DocumentChunkBuilder_CreatesValidChunk()
{
    // Arrange
    var fixture = new Fixture();
    fixture.Customizations.Add(new DocumentChunkBuilder());

    // Act
    var chunk = fixture.Create<DocumentChunk>();

    // Assert
    Assert.That(chunk, Is.Not.Null);
    Assert.That(chunk.Id, Is.Not.Empty);
    Assert.That(chunk.Text, Is.Not.Empty);
    Assert.That(chunk.Embedding, Has.Length.EqualTo(1536));
    Assert.That(chunk.Source, Does.EndWith(".pdf"));
    Assert.That(chunk.Page, Is.GreaterThan(0));
}
```
