using Sibz.Lines;
using UnityEngine;

public class CentreNodePlacer : MonoBehaviour
{
    private LineBehaviour parentLine;

    private void OnEnable()
    {
        parentLine = transform.parent.GetComponent<LineBehaviour>();
    }

    private void Update()
    {
        /*Transform tx = transform;
        Vector3 localPosition = tx.localPosition;
        float len = parentLine.Length / 2 - parentLine.CentreSnapPadding;
        tx.localPosition = new Vector3(
            localPosition.x,
            localPosition.y,
            Mathf.Clamp(parentLine.transform.InverseTransformPoint(parentLine.Cursor.transform.position).z, -len, len));*/
    }
}