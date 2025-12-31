using AutoFixture.Kernel;
using Backend.API.ApplicationCore.DTOs;

namespace Backend.Tests.TestInfrastructure.Builders;

/// <summary>
/// AutoFixture specimen builder for AskQuestionRequest.
/// Creates valid request DTOs with questions that meet validation requirements (minimum 3 characters).
/// </summary>
public class AskQuestionRequestBuilder : ISpecimenBuilder
{
    public object Create(object request, ISpecimenContext context)
    {
        if (request is Type type && type == typeof(AskQuestionRequest))
        {
            var questionTopics = new[]
            {
                "What are the main investment objectives?",
                "What is the risk profile of this fund?",
                "What are the historical performance metrics?",
                "What fees are associated with this investment?",
                "What is the recommended investment horizon?"
            };

            var randomQuestion = questionTopics[Random.Shared.Next(questionTopics.Length)];

            return new AskQuestionRequest
            {
                Question = randomQuestion
            };
        }

        return new NoSpecimen();
    }
}
