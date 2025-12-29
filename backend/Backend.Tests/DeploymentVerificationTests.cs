using System.Net;
using System.Net.Http.Json;

using Backend.API.Models;

using Microsoft.Extensions.Configuration;

namespace Backend.Tests;

/// <summary>
/// Deployment verification tests that call the live Azure API.
/// These tests are marked [Explicit] so they only run manually in Visual Studio.
///
/// Setup: Configure the Azure URL using user secrets:
/// <code>
/// cd backend/Backend.Tests
/// dotnet user-secrets set "DeploymentTests:AzureApiUrl" "https://your-app.azurewebsites.net"
/// </code>
///
/// Note: Azure App Service free tier has cold start delays (up to 30+ seconds)
/// when the app is idle. The first test may take longer due to this warm-up time.
///
/// Important: Azure free tier (F1) has daily quotas: 60 CPU minutes, 1 GB memory.
/// When quota is exceeded, the app returns 503 Service Unavailable until reset (UTC midnight).
/// Check Azure Portal → App Service → Overview for "Quota exceeded" status.
/// </summary>
[TestFixture]
[Explicit("Deployment tests - run manually against live Azure environment")]
public class DeploymentVerificationTests
{
    private HttpClient _httpClient = null!;
    private string _azureApiUrl = null!;

    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets<DeploymentVerificationTests>()
            .Build();

        _azureApiUrl = configuration["DeploymentTests:AzureApiUrl"]
                       ?? throw new InvalidOperationException(
                           "Azure API URL not configured. Set it using:\n" +
                           "cd backend/Backend.Tests\n" +
                           "dotnet user-secrets set \"DeploymentTests:AzureApiUrl\" \"https://your-app.azurewebsites.net\"");

        _httpClient = new HttpClient { BaseAddress = new Uri(_azureApiUrl) };
        _httpClient.Timeout = TimeSpan.FromSeconds(60); // Allow for cold start on free tier
    }

    [OneTimeTearDown]
    public void OneTimeTearDown() => _httpClient?.Dispose();

    [Test]
    public async Task LivenessProbe_ReturnsHealthy()
    {
        // Arrange
        var endpoint = "/health/live";

        // Act
        var response = await _httpClient.GetAsync(endpoint);
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(content, Is.EqualTo("Healthy"));
    }

    [Test]
    public async Task ReadinessProbe_ReturnsHealthy()
    {
        // Arrange
        var endpoint = "/health/ready";

        // Act
        var response = await _httpClient.GetAsync(endpoint);
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(content, Is.EqualTo("Healthy"));
    }

    [Test]
    public async Task AskEndpoint_WithValidQuestion_ReturnsAnswer()
    {
        // Arrange
        var request = new AskRequest { Question = "What is the risk level of the funds?" };

        // Act
        var response = await _httpClient.PostAsJsonAsync("/api/ask", request);
        var result = await response.Content.ReadFromJsonAsync<AskResponse>();

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Answer, Is.Not.Empty);
    }

    [Test]
    public async Task AskEndpoint_WithShortQuestion_ReturnsBadRequest()
    {
        // Arrange
        var request = new AskRequest { Question = "ab" }; // Less than 3 chars minimum

        // Act
        var response = await _httpClient.PostAsJsonAsync("/api/ask", request);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }
}