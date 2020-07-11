using System;
using Sibz.Lines.Tools;
using Unity.Entities;
using UnityEngine;

namespace Sibz.Lines
{
    [RequireComponent(typeof(SnapNotifierBehaviour))]
    public class LineToolBehaviour : PlayerTool
    {
        public LineBehaviour LineBehaviourPrefab;

        public float KnotSpacing = 0.25f;
        public float MinCurveLength = 2f;
        public float MinCurvedLineLength = 1f;

        public Material StraightMaterial;
        public Material CurveMaterial;
        public Material BezierMaterial;

        public GameObject Capsule;

        //private LineTool lineTool;

        private bool placing;
        private SnapNotifierBehaviour snapNotifierBehaviour;

        private Entity lineToolEntity;
//        private SnapNotifierBehaviour snapNotifier;

        public void OnEnable()
        {
            if (!LineBehaviourPrefab)
            {
                throw new NullReferenceException("Must set LinePrefab on Line1Tool!");
            }

            if (!LineDataWorld.World.EntityManager.Exists(lineToolEntity))
            {
                lineToolEntity = LineTool2.New();
            }

            snapNotifierBehaviour = GetComponent<SnapNotifierBehaviour>();

            //lineTool = new LineTool(gameObject, LineBehaviourPrefab.gameObject, this);
            //snapNotifier = GetComponent<SnapNotifierBehaviour>();
        }

        public void OnDisable()
        {
            //lineTool.Cancel();
            placing = false;
        }

        public void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                //lineTool.OriginSnappedToNode = snapNotifier.SnappedTo;
                //lineTool.StartLine();

            }

            if (Input.GetMouseButton(0))
            {
                //lineTool.UpdateLine();

            }

            if (Time.frameCount % 5 == 0 && placing && LineDataWorld.World.EntityManager.GetComponentData<LineTool2>(lineToolEntity).State ==
                LineTool2.ToolState.EditLine)
            {
                LineToolUpdateEvent.New(transform.position);
            }

            if (Input.GetMouseButtonUp(0))
            {
                //lineTool.EndLine();
                if (LineDataWorld.World.EntityManager.GetComponentData<LineTool2>(lineToolEntity).State ==
                    LineTool2.ToolState.Idle)
                {
                    if (snapNotifierBehaviour.SnappedTo)
                    {
                        LineToolCreateLineEvent.New(
                            snapNotifierBehaviour.SnappedTo.transform.position,
                            snapNotifierBehaviour.SnappedTo.GetComponent<NodeBehaviour>().JoinData,
                            -snapNotifierBehaviour.SnappedTo.transform.forward
                            );
                    }
                    else
                    {
                        LineToolCreateLineEvent.New(transform.position);
                    }

                    placing = true;
                }
                else
                {
                    placing = false;
                }
            }

            if (Input.GetMouseButtonUp(2))
            {
                if (LineDataWorld.World.EntityManager.GetComponentData<LineTool2>(lineToolEntity).State ==
                    LineTool2.ToolState.EditLine)
                {
                    placing = false;
                    LineToolFinishEvent.New();
                }
            }

            if (Input.GetKey(KeyCode.LeftControl) && Input.mouseScrollDelta != Vector2.zero)
            {
               // lineTool.AdjustDistance(LineTool.NodeType.Origin, Input.mouseScrollDelta.y * 0.13f);
            }

            if (Input.GetKey(KeyCode.LeftAlt) && Input.mouseScrollDelta != Vector2.zero)
            {
               // lineTool.AdjustDistance(LineTool.NodeType.End, Input.mouseScrollDelta.y * 0.13f);
            }

            if (Input.GetKeyUp(KeyCode.Z))
            {
               // lineTool.Ratio1 += 0.05f;
            }

            if (Input.GetKeyUp(KeyCode.X))
            {
               // lineTool.Ratio1 -= 0.05f;
            }

            if (Input.GetKeyUp(KeyCode.C))
            {
               // lineTool.Ratio2 += 0.05f;
            }

            if (Input.GetKeyUp(KeyCode.V))
            {
                //lineTool.Ratio2 -= 0.05f;
            }

            if (Input.GetKeyUp(KeyCode.Tab))
            {
                //lineTool.ToggleToolMode();
            }
        }
    }
}