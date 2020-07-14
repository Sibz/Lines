using Sibz.Lines;
using UnityEngine;

public class PlayerToolSelectorBehaviour : MonoBehaviour
{
    private readonly PlayerToolSelector selector = new PlayerToolSelector(ToolResourcePaths.Paths);

    private void Update()
    {
        if (Input.GetKeyUp(KeyCode.Alpha1) && selector.CurrentTool.Type != PlayerToolType.Line1)
            selector.SwitchTool(PlayerToolType.Line1);
        else if (Input.GetKeyUp(KeyCode.Escape) || Input.GetMouseButtonDown(1))
            selector.SwitchTool(PlayerToolType.None);
    }
}