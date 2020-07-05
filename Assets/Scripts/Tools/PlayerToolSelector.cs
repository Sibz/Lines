using System.Collections.Generic;
using UnityEngine;

namespace Sibz.Lines
{
    public class PlayerToolSelector
    {
        public class Tool
        {
            public ushort Id { get; internal set; }
            public PlayerToolType Type => (PlayerToolType) Id;

            public GameObject Object =>
                Id == (ushort) PlayerToolType.None ? null : toolObjects[Id];


            private readonly IReadOnlyDictionary<ushort, GameObject> toolObjects;

            public Tool(IReadOnlyDictionary<ushort, GameObject> toolObjects)
            {
                this.toolObjects = toolObjects;
            }
        }

        public Tool CurrentTool { get; }

        private readonly Dictionary<ushort, string> toolResourcePaths;
        private readonly Dictionary<ushort, GameObject> toolObjects = new Dictionary<ushort, GameObject>();

        public PlayerToolSelector(Dictionary<ushort, string> toolResourcePaths)
        {
            this.toolResourcePaths = toolResourcePaths;
            CurrentTool = new Tool(toolObjects);
        }

        public void SwitchTool(PlayerToolType toolType)
        {
            SwitchTool((ushort) toolType);
        }

        public void SwitchTool(ushort toolId)
        {
            if (toolId != (ushort) PlayerToolType.None)
            {
                if (!toolObjects.ContainsKey(toolId))
                {
                    toolObjects.Add(toolId, Object.Instantiate(LoadToolPrefab(toolResourcePaths, toolId)));
                }

                toolObjects[toolId].SetActive(true);
            }

            if (toolObjects.ContainsKey(CurrentTool.Id))
            {
                CurrentTool.Object.SetActive(false);
            }

            CurrentTool.Id = toolId;
        }

        public static GameObject LoadToolPrefab(Dictionary<ushort, string> toolResourcePaths, ushort toolId)
        {
            if (!toolResourcePaths.ContainsKey(toolId))
            {
                throw new NoPathForToolIdException(toolId);
            }

            return Resources.Load<GameObject>(toolResourcePaths[toolId]) ??
                   throw new ToolPrefabNotFoundException(toolResourcePaths[toolId]);
        }
    }

    public class NoPathForToolIdException : System.Exception
    {
        public NoPathForToolIdException(ushort id) : base($"No path for tool id: {id}")
        {
        }
    }

    public class ToolPrefabNotFoundException : System.Exception
    {
        public ToolPrefabNotFoundException(string path) : base($"Prefab not found at path {path}")
        {
        }
    }
}