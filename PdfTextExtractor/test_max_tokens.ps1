# Test script to find optimal maxTokens configuration
# Tests different combinations of DPI and maxTokens

$testConfigs = @(
    @{ DPI = 150; MaxTokens = 500; Description = "DPI 150, 500 tokens - moderate text" },
    @{ DPI = 150; MaxTokens = 1000; Description = "DPI 150, 1000 tokens - more text" },
    @{ DPI = 150; MaxTokens = 1200; Description = "DPI 150, 1200 tokens - full page attempt" },
    @{ DPI = 200; MaxTokens = 250; Description = "DPI 200, 250 tokens - safe baseline" }
)

Write-Host "=== Max Tokens Configuration Testing ===" -ForegroundColor Cyan
Write-Host "Goal: Find configuration that extracts full page text without context overflow" -ForegroundColor Yellow
Write-Host ""

foreach ($config in $testConfigs) {
    Write-Host "Testing: $($config.Description)" -ForegroundColor Green
    Write-Host "  DPI: $($config.DPI), MaxTokens: $($config.MaxTokens)"
    Write-Host "  Expected context usage: ~$([int]($config.DPI * 10 + $config.MaxTokens)) / 4096 tokens"
    Write-Host ""
}

Write-Host "Run manual tests with modified CreateParameters() method" -ForegroundColor Yellow
Write-Host "Update the test file to pass different maxTokens values" -ForegroundColor Yellow
