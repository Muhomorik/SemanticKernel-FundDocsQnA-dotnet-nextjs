using System.ComponentModel.DataAnnotations;

namespace Backend.API.ApplicationCore.Validation;

/// <summary>
/// ASP.NET Core validation attribute to detect obvious prompt injection patterns.
/// Applied to question fields in DTOs to fail-fast on suspicious input.
/// </summary>
/// <remarks>
/// Detection Strategy (conservative, avoid false positives):
/// 1. Suspicious instruction keywords in isolation (e.g., "IGNORE PREVIOUS", "SYSTEM:")
/// 2. Excessive repetition of special characters (>10 consecutive)
///
/// This is a fail-fast layer; actual sanitization happens in UserQuestionSanitizer.
/// </remarks>
public class SafeQuestionAttribute : ValidationAttribute
{
    private static readonly string[] SuspiciousPatterns = new[]
    {
        "IGNORE PREVIOUS",
        "IGNORE ALL PREVIOUS",
        "DISREGARD",
        "SYSTEM:",
        "ASSISTANT:",
        "NEW INSTRUCTIONS:",
        "OVERRIDE",
        "<|im_start|>",
        "<|im_end|>",
        "[INST]",
        "[/INST]"
    };

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not string question)
            return ValidationResult.Success;

        var upperQuestion = question.ToUpperInvariant();

        // Check for suspicious instruction override patterns
        foreach (var pattern in SuspiciousPatterns)
        {
            if (upperQuestion.Contains(pattern))
            {
                return new ValidationResult(
                    $"Question contains potentially unsafe content: '{pattern}'");
            }
        }

        // Check for excessive repetition of special characters (e.g., ">>>>>>>>>>>>>>")
        if (System.Text.RegularExpressions.Regex.IsMatch(question, @"([^\w\s])\1{10,}"))
        {
            return new ValidationResult(
                "Question contains suspicious character repetition");
        }

        return ValidationResult.Success;
    }
}
