using System;
using UnityEngine;

namespace Sibz.Lines
{
    [RequireComponent(typeof(SnapNotifierBehaviour))]
    public class LineToolBehaviour : PlayerTool
    {
        public LineBehaviour LineBehaviourPrefab;

        public float KnotSpacing = 0.25f;
        public float MinCurveLength = 1f;
        public float MinCurvedLineLength = 1f;

        public Material StraightMaterial;
        public Material CurveMaterial;
        public Material BezierMaterial;

        public GameObject Capsule;

        private LineTool lineTool;
//        private SnapNotifierBehaviour snapNotifier;

        public void OnEnable()
        {
            if (!LineBehaviourPrefab)
            {
                throw new NullReferenceException("Must set LinePrefab on Line1Tool!");
            }

            lineTool = new LineTool(gameObject, LineBehaviourPrefab.gameObject, this);
            //snapNotifier = GetComponent<SnapNotifierBehaviour>();
        }

        public void OnDisable()
        {
            lineTool.Cancel();
        }

        public void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                //lineTool.OriginSnappedToNode = snapNotifier.SnappedTo;
                lineTool.StartLine();
            }

            if (Input.GetMouseButton(0))
            {
                lineTool.UpdateLine();
            }

            if (Input.GetMouseButtonUp(0))
            {
                lineTool.EndLine();
            }

            if (Input.GetKey(KeyCode.LeftControl) && Input.mouseScrollDelta != Vector2.zero)
            {
                lineTool.AdjustDistance(LineTool.NodeType.Origin, Input.mouseScrollDelta.y * 0.03f);
            }

            if (Input.GetKey(KeyCode.LeftAlt) && Input.mouseScrollDelta != Vector2.zero)
            {
                lineTool.AdjustDistance(LineTool.NodeType.End, Input.mouseScrollDelta.y * 0.03f);
            }

            if (Input.GetKeyUp(KeyCode.Tab))
            {
                lineTool.ToggleToolMode();
            }
        }
    }
}