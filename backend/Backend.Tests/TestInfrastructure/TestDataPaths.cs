namespace Backend.Tests.TestInfrastructure;

/// <summary>
/// Resolves paths to test data files relative to the test assembly output directory.
/// Files must be configured as Content → CopyToOutputDirectory in the .csproj.
/// </summary>
public static class TestDataPaths
{
    private static readonly string TestDataDir = Path.Combine(
        AppContext.BaseDirectory, "TestData");

    public static string TestEmbeddingsJson =>
        Path.Combine(TestDataDir, "test_embeddings.json");
}
