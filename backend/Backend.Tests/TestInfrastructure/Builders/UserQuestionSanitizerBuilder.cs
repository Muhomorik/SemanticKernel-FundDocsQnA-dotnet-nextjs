using AutoFixture.Kernel;
using Backend.API.Domain.Services;

namespace Backend.Tests.TestInfrastructure.Builders;

/// <summary>
/// AutoFixture specimen builder for UserQuestionSanitizer.
/// No complex setup needed - just instantiate the pure domain service.
/// </summary>
public class UserQuestionSanitizerBuilder : ISpecimenBuilder
{
    public object Create(object request, ISpecimenContext context)
    {
        if (request is Type type && type == typeof(UserQuestionSanitizer))
        {
            return new UserQuestionSanitizer();
        }

        return new NoSpecimen();
    }
}
