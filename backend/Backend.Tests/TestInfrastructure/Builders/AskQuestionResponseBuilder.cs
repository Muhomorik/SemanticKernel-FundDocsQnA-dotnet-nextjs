using AutoFixture.Kernel;
using Backend.API.ApplicationCore.DTOs;

namespace Backend.Tests.TestInfrastructure.Builders;

/// <summary>
/// AutoFixture specimen builder for AskQuestionResponse.
/// Creates valid response DTOs with answers and source references.
/// </summary>
public class AskQuestionResponseBuilder : ISpecimenBuilder
{
    public object Create(object request, ISpecimenContext context)
    {
        if (request is Type type && type == typeof(AskQuestionResponse))
        {
            var randomText = (string)context.Resolve(typeof(string));
            var answer = $"Based on the available information, {randomText}";
            var sources = Enumerable.Range(0, Random.Shared.Next(1, 4))
                .Select(_ => new SourceReferenceDto
                {
                    File = $"document-{Guid.NewGuid()}.pdf",
                    Page = Random.Shared.Next(1, 100)
                })
                .ToList();

            return new AskQuestionResponse
            {
                Answer = answer,
                Sources = sources
            };
        }

        return new NoSpecimen();
    }
}
