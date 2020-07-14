using UnityEngine;

namespace Sibz.Lines
{
    public class SnapNotifierBehaviour : MonoBehaviour
    {
        public GameObject SnappedTo;

        public void SnappedToNode(GameObject node)
        {
            SnappedTo = node;
        }

        public void UnSnapped()
        {
            SnappedTo = null;
        }
    }
}