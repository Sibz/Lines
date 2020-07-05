using UnityEngine;

namespace Sibz.Lines
{
    public abstract class LineBase : MonoBehaviour
    {
        public abstract void BeginCreation(GameObject snappedTo = null);
        public abstract void MoveEndNodeAndRebuildMesh(Vector3 position, GameObject otherNode = null);
        public abstract void CompleteCreation();
    }
}