﻿
using Microsoft.Bot.Builder.M365.AI.Planner;
using Microsoft.Bot.Builder.M365.AI.Prompt;
using Microsoft.Bot.Builder.M365.AI;
using Moq;
using Microsoft.Bot.Builder.M365.Exceptions;
using AIException = Microsoft.SemanticKernel.AI.AIException;
using Microsoft.Bot.Builder.M365.AI.Action;
using Microsoft.Extensions.Logging;
using Microsoft.Bot.Builder.M365.AI.Moderator;

namespace Microsoft.Bot.Builder.M365.Tests.AITests
{
    public class OpenAIPlannerTests
    {
        [Fact]
        public async void Test_GeneratePlan_PromptCompletionRateLimited_ShouldRedirectToRateLimitedAction()
        {
            // Arrange
            var apiKey = "randomApiKey";
            var model = "randomModelId";

            var options = new OpenAIPlannerOptions(apiKey,model);
            var turnContextMock = new Mock<ITurnContext>();
            var turnStateMock = new Mock<TurnState>();
            var moderatorMock = new Mock<IModerator<TurnState>>();

            var promptTemplate = new PromptTemplate(
                "prompt",
                new PromptTemplateConfiguration
                {
                    Completion =
                    {
                        MaxTokens = 2000,
                        Temperature = 0.2,
                        TopP = 0.5,
                    }
                }
            );

            static string rateLimitedFunc() => throw new PlannerException("", new AIException(AIException.ErrorCodes.Throttling));
            var planner = new CustomCompletePromptOpenAIPlanner<TurnState, OpenAIPlannerOptions>(options, rateLimitedFunc);
            var aiOptions = new AIOptions<TurnState>(planner, new PromptManager<TurnState>(), moderatorMock.Object);
            
            // Act
            var result = await planner.GeneratePlanAsync(turnContextMock.Object, turnStateMock.Object, promptTemplate, aiOptions);

            // Assert
            Assert.Single(result.Commands);
            Assert.Equal(AITypes.DoCommand, result.Commands[0].Type);

            var doCommand = (PredictedDoCommand)result.Commands[0];
            Assert.Equal(DefaultActionTypes.RateLimitedActionName, doCommand.Action);
            Assert.Empty(doCommand.Entities);

        }

        [Fact]
        public async void Test_GeneratePlan_PromptCompletionFailed_ThrowsException()
        {
            // Arrange
            var apiKey = "randomApiKey";
            var model = "randomModelId";

            var options = new OpenAIPlannerOptions(apiKey, model);
            var turnContextMock = new Mock<ITurnContext>();
            var turnStateMock = new Mock<TurnState>();
            var moderatorMock = new Mock<IModerator<TurnState>>();

            var promptTemplate = new PromptTemplate(
                "prompt",
                new PromptTemplateConfiguration
                {
                    Completion =
                    {
                        MaxTokens = 2000,
                        Temperature = 0.2,
                        TopP = 0.5,
                    }
                }
            );

            static string throwsExceptionFunc() => throw new PlannerException("Exception Message");
            var planner = new CustomCompletePromptOpenAIPlanner<TurnState, OpenAIPlannerOptions>(options, throwsExceptionFunc);
            var aiOptions = new AIOptions<TurnState>(planner, new PromptManager<TurnState>(), moderatorMock.Object);

            // Act
            var exception = await Assert.ThrowsAsync<PlannerException>(async () => await planner.GeneratePlanAsync(turnContextMock.Object, turnStateMock.Object, promptTemplate, aiOptions));

            // Assert
            Assert.Equal("Exception Message", exception.Message);

        }

        [Fact]
        public async void Test_GeneratePlan_PromptCompletionEmptyStringResponse_ReturnsEmptyPlan()
        {
            // Arrange
            var apiKey = "randomApiKey";
            var model = "randomModelId";

            var options = new OpenAIPlannerOptions(apiKey, model);
            var turnContextMock = new Mock<ITurnContext>();
            var turnStateMock = new Mock<TurnState>();
            var moderatorMock = new Mock<IModerator<TurnState>>();

            var promptTemplate = new PromptTemplate(
                "prompt",
                new PromptTemplateConfiguration
                {
                    Completion =
                    {
                        MaxTokens = 2000,
                        Temperature = 0.2,
                        TopP = 0.5,
                    }
                }
            );

            static string emptyStringFunc() => String.Empty;
            var planner = new CustomCompletePromptOpenAIPlanner<TurnState, OpenAIPlannerOptions>(options, emptyStringFunc);
            var aiOptions = new AIOptions<TurnState>(planner, new PromptManager<TurnState>(), moderatorMock.Object);

            // Act
            var result = await planner.GeneratePlanAsync(turnContextMock.Object, turnStateMock.Object, promptTemplate, aiOptions);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Commands);
        }

        [Fact]
        public async void Test_GeneratePlan_PromptCompletion_OneSayPerTurn()
        {
            // Arrange
            var apiKey = "randomApiKey";
            var model = "randomModelId";

            var options = new OpenAIPlannerOptions(apiKey, model);
            options.OneSayPerTurn = true;

            var turnContextMock = new Mock<ITurnContext>();
            var turnStateMock = new Mock<TurnState>();
            var moderatorMock = new Mock<IModerator<TurnState>>();

            var promptTemplate = new PromptTemplate(
                "prompt",
                new PromptTemplateConfiguration
                {
                    Completion =
                    {
                        MaxTokens = 2000,
                        Temperature = 0.2,
                        TopP = 0.5,
                    }
                }
            );

            string multipleSayCommands = @"{ 'type':'plan','commands':[{'type':'SAY','response':'responseValueA'}, {'type':'SAY','response':'responseValueB'}]}";
            string multipleSayCommandsFunc() => multipleSayCommands;
            var planner = new CustomCompletePromptOpenAIPlanner<TurnState, OpenAIPlannerOptions>(options, multipleSayCommandsFunc);
            var aiOptions = new AIOptions<TurnState>(planner, new PromptManager<TurnState>(), moderatorMock.Object);


            // Act
            var result = await planner.GeneratePlanAsync(turnContextMock.Object, turnStateMock.Object, promptTemplate, aiOptions);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Commands);

            var sayCommand = (PredictedSayCommand)result.Commands[0];
            Assert.Equal("responseValueA", sayCommand.Response);
        }

        [Fact]
        public async void Test_GeneratePlan_Simple()
        {
            // Arrange
            var apiKey = "randomApiKey";
            var model = "randomModelId";

            var options = new OpenAIPlannerOptions(apiKey, model);

            var turnContextMock = new Mock<ITurnContext>();
            var turnStateMock = new Mock<TurnState>();
            var moderatorMock = new Mock<IModerator<TurnState>>();

            var promptTemplate = new PromptTemplate(
                "prompt",
                new PromptTemplateConfiguration
                {
                    Completion =
                    {
                        MaxTokens = 2000,
                        Temperature = 0.2,
                        TopP = 0.5,
                    }
                }
            );

            string simplePlan = @"{ 'type':'plan','commands':[{'type':'SAY','response':'responseValueA'}, {'type':'DO', 'action': 'actionName'}]}";
            string multipleSayCommandsFunc() => simplePlan;
            var planner = new CustomCompletePromptOpenAIPlanner<TurnState, OpenAIPlannerOptions>(options, multipleSayCommandsFunc);

            var aiOptions = new AIOptions<TurnState>(planner, new PromptManager<TurnState>(), moderatorMock.Object);

            // Act
            var result = await planner.GeneratePlanAsync(turnContextMock.Object, turnStateMock.Object, promptTemplate, aiOptions);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Commands.Count);

            var sayCommand = (PredictedSayCommand)result.Commands[0];
            Assert.Equal("responseValueA", sayCommand.Response);

            var doCommand = (PredictedDoCommand)result.Commands[1];
            Assert.Equal("actionName", doCommand.Action);
        }

        private class CustomCompletePromptOpenAIPlanner<TState, TOptions> : OpenAIPlanner<TState, TOptions>
            where TState : TurnState
            where TOptions : OpenAIPlannerOptions
        {
            private Func<string> customFunction;

            public CustomCompletePromptOpenAIPlanner(TOptions options, Func<string> customFunction, ILogger? logger = null) : base(options, logger)
            {
                this.customFunction = customFunction;
            }

            public override Task<string> CompletePromptAsync(ITurnContext turnContext, TState turnState, PromptTemplate promptTemplate, AIOptions<TState> options, CancellationToken cancellationToken = default)
            {
                return Task.FromResult(customFunction.Invoke());
            }

        }
    }
}
