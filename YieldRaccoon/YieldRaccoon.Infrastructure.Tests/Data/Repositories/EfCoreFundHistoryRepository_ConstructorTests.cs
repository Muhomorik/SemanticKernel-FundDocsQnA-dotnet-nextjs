using NUnit.Framework;
using YieldRaccoon.Infrastructure.Data.Repositories;

namespace YieldRaccoon.Infrastructure.Tests.Data.Repositories;

[TestFixture]
[TestOf(typeof(EfCoreFundHistoryRepository))]
public class EfCoreFundHistoryRepository_ConstructorTests
{
    [Test]
    public void Constructor_NullContext_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.That(
            () => new EfCoreFundHistoryRepository(null!),
            Throws.ArgumentNullException.With.Property("ParamName").EqualTo("context"));
    }
}
