using System;
using System.Collections.Generic;
using UnityEngine;

namespace Sibz.Lines
{
    public class PlayerToolSelector
    {
        private readonly Dictionary<ushort, GameObject> toolObjects = new Dictionary<ushort, GameObject>();
        private readonly Dictionary<ushort, string>     toolResourcePaths;

        public Tool CurrentTool { get; }

        public PlayerToolSelector(Dictionary<ushort, string> toolResourcePaths)
        {
            this.toolResourcePaths = toolResourcePaths;
            CurrentTool            = new Tool(toolObjects);
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
                    toolObjects.Add(toolId, UnityEngine.Object.Instantiate(LoadToolPrefab(toolResourcePaths, toolId)));

                toolObjects[toolId].SetActive(true);
            }

            if (toolObjects.ContainsKey(CurrentTool.Id)) CurrentTool.Object.SetActive(false);

            CurrentTool.Id = toolId;
        }

        public static GameObject LoadToolPrefab(Dictionary<ushort, string> toolResourcePaths, ushort toolId)
        {
            if (!toolResourcePaths.ContainsKey(toolId)) throw new NoPathForToolIdException(toolId);

            return Resources.Load<GameObject>(toolResourcePaths[toolId]) ??
                   throw new ToolPrefabNotFoundException(toolResourcePaths[toolId]);
        }

        public class Tool
        {
            private readonly IReadOnlyDictionary<ushort, GameObject> toolObjects;

            public ushort         Id   { get; internal set; }
            public PlayerToolType Type => (PlayerToolType) Id;

            public GameObject Object =>
                Id == (ushort) PlayerToolType.None ? null : toolObjects[Id];

            public Tool(IReadOnlyDictionary<ushort, GameObject> toolObjects)
            {
                this.toolObjects = toolObjects;
            }
        }
    }

    public class NoPathForToolIdException : Exception
    {
        public NoPathForToolIdException(ushort id) : base($"No path for tool id: {id}")
        {
        }
    }

    public class ToolPrefabNotFoundException : Exception
    {
        public ToolPrefabNotFoundException(string path) : base($"Prefab not found at path {path}")
        {
        }
    }
}