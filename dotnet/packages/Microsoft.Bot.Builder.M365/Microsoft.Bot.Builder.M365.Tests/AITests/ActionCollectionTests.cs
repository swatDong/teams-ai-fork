﻿using Microsoft.Bot.Builder.M365.AI.Action;
using Microsoft.Bot.Builder.M365.Tests.TestUtils;

namespace Microsoft.Bot.Builder.M365.Tests.AITests
{
    public class ActionCollectionTests
    {
        [Fact]
        public void Test_Simple()
        {
            // Arrange
            IActionCollection<TestTurnState> actionCollection = new ActionCollection<TestTurnState>();
            string name = "action";
            ActionHandler<TestTurnState> handler = (turnContext, turnState, data, action) => Task.FromResult(true);
            bool allowOverrides = true;

            // Act
            actionCollection.SetAction(name, handler, allowOverrides);
            ActionEntry<TestTurnState> entry = actionCollection.GetAction(name);

            // Assert
            Assert.True(actionCollection.HasAction(name));
            Assert.NotNull(entry);
            Assert.Equal(name, entry.Name);
            Assert.Equal(handler, entry.Handler);
            Assert.Equal(allowOverrides, entry.AllowOverrides);
        }

        [Fact]
        public void Test_Set_NonOverridable_Action_Throws_Exception()
        {
            // Arrange
            IActionCollection<TestTurnState> actionCollection = new ActionCollection<TestTurnState>();
            string name = "action";
            ActionHandler<TestTurnState> handler = (turnContext, turnState, data, action) => Task.FromResult(true);
            bool allowOverrides = false;
            actionCollection.SetAction(name, handler, allowOverrides);

            // Act
            var func = () => actionCollection.SetAction(name, handler, allowOverrides);

            // Assert
            Exception ex = Assert.Throws<ArgumentException>(() => func());
            Assert.Equal($"Action {name} already exists and does not allow overrides", ex.Message);
        }

        [Fact]
        public void Test_Get_NonExistent_Action()
        {
            // Arrange
            IActionCollection<TestTurnState> actionCollection = new ActionCollection<TestTurnState>();
            var nonExistentAction = "non existent action";

            // Act
            var func = () => actionCollection.GetAction(nonExistentAction);

            // Assert
            Exception ex = Assert.Throws<ArgumentException>(() => func());
            Assert.Equal($"`{nonExistentAction}` action does not exist", ex.Message);
        }

        [Fact]
        public void Test_HasAction_False()
        {
            // Arrange
            IActionCollection<TestTurnState> actionCollection = new ActionCollection<TestTurnState>();
            var nonExistentAction = "non existent action";

            // Act
            bool hasAction = actionCollection.HasAction(nonExistentAction);

            // Assert
            Assert.False(hasAction);
        }

        [Fact]
        public void Test_HasAction_True()
        {
            // Arrange
            IActionCollection<TestTurnState> actionCollection = new ActionCollection<TestTurnState>();
            ActionHandler<TestTurnState> handler = (turnContext, turnState, data, action) => Task.FromResult(true);
            var name = "actionName";

            // Act
            actionCollection.SetAction(name, handler, true);
            bool hasAction = actionCollection.HasAction(name);

            // Assert
            Assert.True(hasAction);
        }
    }
}
