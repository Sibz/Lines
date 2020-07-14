using System.Collections.Generic;
using NUnit.Framework;

namespace Sibz.Lines.Tests
{
    public class PlayerToolSelectorTests
    {
        [Test]
        public void WhenLoadingInvalidToolId_ShouldThrow()
        {
            Assert.Catch<NoPathForToolIdException>(() =>
                                                       PlayerToolSelector.LoadToolPrefab(ToolResourcePaths.Paths, 532));
        }

        [Test]
        public void WhenLoadingValidToolId_ShouldGetObject()
        {
            Assert.IsNotNull(PlayerToolSelector.LoadToolPrefab(ToolResourcePaths.Paths, 1));
        }

        [Test]
        public void WhenLoadingInvalidPath_ShouldThrow()
        {
            Assert.Catch<ToolPrefabNotFoundException>(
                                                      () =>
                                                          PlayerToolSelector
                                                             .LoadToolPrefab(new Dictionary<ushort, string>
                                                                             {
                                                                                 {1, "Prefabs/Invalid"}
                                                                             }, 1));
        }

        [Test]
        public void WhenOneShouldSelectFirstTool()
        {
            var toolSelector = new PlayerToolSelector(ToolResourcePaths.Paths);
            toolSelector.SwitchTool(1);
            Assert.IsTrue(toolSelector.CurrentTool.Id == (ushort) PlayerToolType.Line1);
        }

        [Test]
        public void WhenOneShouldHaveInstanceOfLine1Tool()
        {
            var toolSelector = new PlayerToolSelector(ToolResourcePaths.Paths);
            toolSelector.SwitchTool(1);
            Assert.IsNotNull(toolSelector.CurrentTool.Object, "CurrentToolObject Not Set");
            Assert.IsNotNull(toolSelector.CurrentTool.Object.GetComponent<LineToolBehaviour>());
        }

        [Test]
        public void WhenToolSetToNone_ShouldDeactivateCurrentTool()
        {
            var toolSelector = new PlayerToolSelector(ToolResourcePaths.Paths);
            toolSelector.SwitchTool(1);
            var currentTool = toolSelector.CurrentTool.Object;
            toolSelector.SwitchTool((ushort) PlayerToolType.None);
            Assert.IsFalse(currentTool.activeSelf);
        }
    }
}