using System;
using Sibz.Lines.ECS;
using Sibz.Lines.ECS.Behaviours;
using Sibz.Lines.ECS.Components;
using Sibz.Lines.ECS.Enums;
using Sibz.Lines.ECS.Events;
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

        private SnapNotifierBehaviour snapNotifier;

        private Entity lineToolEntity;

        private LineTool LineTool =>
            LineWorld.Em.Exists(lineToolEntity)
                ? LineWorld.Em.GetComponentData<LineTool>(lineToolEntity)
                : throw new InvalidOperationException("Tool singleton not found");

        private EcsLineBehaviour EditingLineBehaviour =>
            LineWorld.Em.Exists(lineToolEntity)
            && LineTool.State == LineToolState.Editing
            && LineWorld.Em.Exists(LineTool.Data.LineEntity)
                ? LineWorld.Em.GetComponentObject<EcsLineBehaviour>(LineTool.Data.LineEntity)
                : null;


        private bool draggingNewLine;

        public void OnEnable()
        {
            if (!LineBehaviourPrefab)
            {
                throw new NullReferenceException("Must set LinePrefab on Line1Tool!");
            }

            lineToolEntity = LineWorld.Em.Exists(lineToolEntity) ? lineToolEntity : LineTool.New();
            if (LineWorld.Em.HasComponent<Disabled>(lineToolEntity))
            {
                LineWorld.Em.RemoveComponent<Disabled>(lineToolEntity);
            }

            snapNotifier = GetComponent<SnapNotifierBehaviour>();
        }

        public void OnDisable()
        {
            if (LineWorld.World.IsCreated && LineWorld.Em.Exists(lineToolEntity))
            {
                if (EditingLineBehaviour)
                {
                    Destroy(EditingLineBehaviour.gameObject);
                }

                LineWorld.Em.SetComponentData(lineToolEntity, LineTool.Default());
                LineWorld.Em.AddComponent<Disabled>(lineToolEntity);
            }
        }

        public void Update()
        {
            if ((Input.GetMouseButtonDown(2) || Input.GetKeyDown(KeyCode.Return)) && EditingLineBehaviour)
            {
                NewLineCompleteEvent.New();
            }

            if (Input.GetMouseButtonDown(0))
            {
                if (LineTool.State == LineToolState.Idle)
                {
                    EcsLineNodeBehaviour node;
                    if (snapNotifier.SnappedTo
                        && (node = snapNotifier.SnappedTo.GetComponent<EcsLineNodeBehaviour>()) != null)
                    {
                        NewLineEvent.New(node.transform.position, node.JoinPoint);
                    }
                    else
                    {
                        NewLineEvent.New(transform.position);
                    }

                    draggingNewLine = true;
                }
            }
            if ((Input.GetMouseButton(0) && draggingNewLine && EditingLineBehaviour)
                     || (Input.GetMouseButtonDown(0) && !draggingNewLine && EditingLineBehaviour))
            {
                EcsLineNodeBehaviour node;
                if (snapNotifier.SnappedTo
                    && (node = snapNotifier.SnappedTo.GetComponent<EcsLineNodeBehaviour>()) != null)
                {
                    NewLineUpdateEvent.New(EditingLineBehaviour.EndNode2.JoinPoint, transform.position, node.JoinPoint);
                }
                else
                {
                    NewLineUpdateEvent.New(EditingLineBehaviour.EndNode2.JoinPoint, transform.position);
                }
            }

            if (Input.GetMouseButtonUp(0))
            {
                draggingNewLine = false;
            }

            if (Input.GetKey(KeyCode.LeftControl) && Input.mouseScrollDelta != Vector2.zero)
            {
                LineToolModChangeEvent.New(new LineToolData.ToolModifiers
                {
                    From =
                    {
                        Size = Input.mouseScrollDelta.y * 0.13f
                    }
                });
            }

            if (Input.GetKey(KeyCode.LeftAlt) && Input.mouseScrollDelta != Vector2.zero)
            {
                LineToolModChangeEvent.New(new LineToolData.ToolModifiers
                {
                    To =
                    {
                        Size = Input.mouseScrollDelta.y * 0.13f
                    }
                });
            }

            if (Input.GetKeyUp(KeyCode.Z))
            {
                LineToolModChangeEvent.New(new LineToolData.ToolModifiers
                {
                    From =
                    {
                        Ratio = 0.05f
                    }
                });
            }

            if (Input.GetKeyUp(KeyCode.X))
            {
                LineToolModChangeEvent.New(new LineToolData.ToolModifiers
                {
                    From =
                    {
                        Ratio = -0.05f
                    }
                });
            }

            if (Input.GetKeyUp(KeyCode.C))
            {
                LineToolModChangeEvent.New(new LineToolData.ToolModifiers
                {
                    To =
                    {
                        Ratio = 0.05f
                    }
                });
            }

            if (Input.GetKeyUp(KeyCode.V))
            {
                LineToolModChangeEvent.New(new LineToolData.ToolModifiers
                {
                    To =
                    {
                        Ratio = -0.05f
                    }
                });
            }

            /*if (Input.GetKeyUp(KeyCode.Tab))
            {
                //lineTool.ToggleToolMode();
            }*/
        }
    }
}