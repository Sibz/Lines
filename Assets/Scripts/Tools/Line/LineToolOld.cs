using System;
using Unity.Mathematics;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Sibz.Lines
{
    public class LineToolOld
    {
        public LineBehaviour CurrentLine;

        private float ratio1 = 1f;

        public float Ratio1
        {
            get => ratio1;
            set => ratio1 = math.clamp(value, 0.25f, 1.75f);
        }

        private float ratio2 = 1f;

        public float Ratio2
        {
            get => ratio2;
            set => ratio2 = math.clamp(value, 0.25f, 1.75f);
        }

        private const float DefaultOriginDistance = 4f;
        private const float DefaultEndDistance = 4f;

        private readonly GameObject linePrefab;
        private readonly GameObject cursor;
        private readonly LineToolBehaviour tool;

        private float3x3 curve1, curve2;

        private readonly SnapNotifierBehaviour snapNotifier;
        private GameObject originSnappedToNode;
        private GameObject endSnappedToNode;

        private MeshRenderer meshRenderer;

        public LineToolMode Mode { get; set; }
        public float EndDistance { get; set; } = DefaultOriginDistance;
        public float OriginDistance { get; set; } = DefaultEndDistance;

        private LocalToolMode localMode;

        private enum LocalToolMode
        {
            Straight,
            StraightOriginCurve,
            StraightEndCurve,
            StraightOriginAndEndCurves
        }

        public void ResetTool()
        {
            CurrentLine = null;
            OriginDistance = DefaultOriginDistance;
            EndDistance = DefaultEndDistance;
            Ratio1 = 1f;
        }

        public void SetToolMode(LineToolMode mode)
        {
            Mode = mode;
            switch (mode)
            {
                case LineToolMode.Straight:
                    meshRenderer.sharedMaterial = tool.StraightMaterial;
                    break;
                case LineToolMode.Curve:
                    meshRenderer.sharedMaterial = tool.CurveMaterial;
                    break;
                case LineToolMode.CubicBezier:
                    meshRenderer.sharedMaterial = tool.BezierMaterial;
                    break;
            }

            if (!CurrentLine)
                return;
            if (mode == LineToolMode.CubicBezier || mode == LineToolMode.Curve)
            {
                OriginDistance = 0.5f;
                EndDistance = 0.5f;
            }
        }

        public void ToggleToolMode()
        {
            SetToolMode(Mode switch
            {
                LineToolMode.Curve => LineToolMode.CubicBezier,
                LineToolMode.CubicBezier => LineToolMode.Straight,
                LineToolMode.Straight => LineToolMode.Curve,
                var _ => throw new NotImplementedException()
            });
        }

        public LineToolOld(GameObject cursor, GameObject linePrefab, LineToolBehaviour tool)
        {
            this.linePrefab = linePrefab == null
                ? throw new System.ArgumentNullException(nameof(linePrefab))
                : linePrefab;
            this.cursor = cursor == null ? throw new System.ArgumentNullException(nameof(cursor)) : cursor;
            this.tool = tool;
            snapNotifier = tool.GetComponent<SnapNotifierBehaviour>();
            meshRenderer = tool.Capsule.GetComponent<MeshRenderer>();
        }

        public void StartLine()
        {
            Transform tx = cursor.transform;
            Quaternion rotation = tx.rotation;
            Vector3 position = tx.position;

            originSnappedToNode = snapNotifier.SnappedTo;

            CurrentLine = Object.Instantiate(linePrefab.gameObject, position, rotation)
                .GetComponent<LineBehaviour>();

            CurrentLine.OriginNode.transform.position = position;

            CurrentLine.OriginNode.transform.rotation =
                originSnappedToNode
                    ? Quaternion.LookRotation(
                        CurrentLine.transform.InverseTransformDirection(-originSnappedToNode.transform.forward))
                    : rotation;

            CurrentLine.Line.NodeCollidersEnabled = false;
        }

        public void AdjustDistance(NodeType nodeType, float adjustment)
        {
            if (!CurrentLine)
            {
                return;
            }

            switch (nodeType)
            {
                case NodeType.Origin:
                    OriginDistance = math.clamp(OriginDistance + adjustment, tool.MinCurveLength, CurrentLine.Line.Length);
                    break;
                case NodeType.End:
                    EndDistance = math.clamp(EndDistance + adjustment, tool.MinCurveLength, CurrentLine.Line.Length);
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        public enum NodeType
        {
            Origin,
            End
        }

        public void UpdateLine()
        {
            SetEndSnappedNode();
            SetToolMode();
            MoveEndNode();
            AdjustLinePosition();
            UpdateCurve();
            UpdateKnots();
            CurrentLine.Line.RebuildMesh();
        }

        public void SetEndSnappedNode()
        {
            if (snapNotifier.SnappedTo != originSnappedToNode)
            {
                endSnappedToNode = snapNotifier.SnappedTo;
            }
            else
            {
                endSnappedToNode = null;
            }
        }

        private void SetToolMode()
        {
            if (CurrentLine.Line.Length < tool.MinCurvedLineLength)
            {
                localMode = LocalToolMode.Straight;
            }
            else if (endSnappedToNode && originSnappedToNode)
            {
                localMode = LocalToolMode.StraightOriginAndEndCurves;
            }
            else if (endSnappedToNode)
            {
                localMode = LocalToolMode.StraightEndCurve;
            }
            else if (originSnappedToNode)

            {
                localMode = LocalToolMode.StraightOriginCurve;
            }
            else
            {
                localMode = LocalToolMode.Straight;
            }
        }

        private void UpdateCurve()
        {
            Transform originTx = CurrentLine.OriginNode.transform;
            Transform endTx = CurrentLine.EndNode.transform;
            float3 lineEndA = originTx.localPosition;
            float3 lineEndB = endTx.localPosition;

            // Two curves  b1, b2
            float3x3 b1, b2;

            // c0 is the origin
            b1.c0 = lineEndA;
            b2.c0 = lineEndB;

            float3x2 forwards = new float3x2(
                CurrentLine.transform.InverseTransformDirection(originTx.forward),
                CurrentLine.transform.InverseTransformDirection(endTx.forward));

            float2 distances = new float2(
                Distance(lineEndA, lineEndB, forwards.c0),
                Distance(lineEndB, lineEndA, forwards.c1));

            float2 scales = new float2(
                Scale(OriginDistance, distances.x),
                Scale(EndDistance, distances.y));

            if (localMode == LocalToolMode.StraightOriginCurve)
            {
                distances.y = 0f;
                b1.c1 = GetOrigin();
                // Do Rotate
                endTx.LookAt(CurrentLine.transform.TransformPoint(b1.c1));
                b1.c2 = Target(b1.c1, b2.c0, distances.x, distances.y, scales.x, scales.y, ratio1);
                b2.c2 = b2.c1 = b2.c0;
            }
            else if (localMode == LocalToolMode.StraightEndCurve)
            {
                distances.x = 0f;
                b2.c1 = GetEnd();
                // Do Rotate
                originTx.LookAt(b2.c1);
                b2.c2 = Target(b2.c1, b1.c0, distances.y, distances.x, scales.y, scales.x, ratio2);
                b1.c2 = b1.c1 = b1.c0;
            }
            else
            {
                if (localMode == LocalToolMode.Straight)
                {
                    if (!originSnappedToNode)
                    {
                        originTx.LookAt(endTx.position);
                    }

                    if (!endSnappedToNode)
                    {
                        endTx.LookAt(originTx.position);
                    }
                }

                b1.c1 = GetOrigin();
                b2.c1 = GetEnd();
                b1.c2 = Target(b1.c1, b2.c1, distances.x, distances.y, scales.x, scales.y, ratio1);
                b2.c2 = Target(b2.c1, b1.c1, distances.y, distances.x, scales.y, scales.x, ratio2);
            }

            curve1 = b1;
            curve2 = b2;

            float3 GetOrigin() => GetControlPoint(forwards.c0, lineEndA, distances.x, scales.x, ratio1);
            float3 GetEnd() => GetControlPoint(forwards.c1, lineEndB, distances.y, scales.y, ratio2);

            static float3 GetControlPoint(float3 f, float3 p1, float h, float s, float r) => p1 + f * h * s * r;

            static float3 Target(float3 c1, float3 c2, float h1, float h2, float s1, float s2, float r)
            {
                float d = Dist(c1, c2);
                float ht = h1 * s1 * (2 - r) + h2 * s2 * (2 - r);
                return c1 + Normalize(c2 - c1) * (ht > d ? d * (h1 * s1 * (2 - r) / ht) : h1 * s1 * (2 - r));
            }

            static float Distance(float3 p1, float3 p2, float3 forwards)
            {
                float angle = AngleD(forwards, Normalize(p2 - p1));
                angle = angle > 90 ? 90 + (angle - 90) / 2f : angle;
                return Abs((Dist(p2, p1) / SinD(180 - 2 * angle)) * SinD(angle));
            }

            static float Abs(float a) => math.abs(a);
            static float3 Normalize(float3 a) => math.normalize(a);
            static float Dist(float3 a, float3 b) => math.distance(a, b);
            static float SinD(float a) => math.sin(math.PI / 180 * a);
            static float AngleD(float3 a, float3 b) => LineHelpers.AngleDegrees(a, b);
            static float Scale(float units, float distance) => math.min(units / distance, 1);

            Debug.DrawLine(CurrentLine.transform.TransformPoint(b1.c0),
                CurrentLine.transform.TransformPoint(b1.c0) + originTx.forward * 1f, Color.white, 0.25f);
            Debug.DrawLine(CurrentLine.transform.TransformPoint(b1.c0),
                CurrentLine.transform.TransformPoint(b1.c0) + Vector3.up * 1.75f, Color.cyan, 0.25f);
            Debug.DrawLine(CurrentLine.transform.TransformPoint(b1.c1),
                CurrentLine.transform.TransformPoint(b1.c1) + Vector3.up * 1.5f, Color.red, 0.25f);
            Debug.DrawLine(CurrentLine.transform.TransformPoint(b1.c2),
                CurrentLine.transform.TransformPoint(b1.c2) + Vector3.up * 1.25f, Color.cyan, 0.25f);

            Debug.DrawLine(CurrentLine.transform.TransformPoint(b2.c0),
                CurrentLine.transform.TransformPoint(b2.c0) + endTx.forward * 1f, Color.white, 0.25f);
            Debug.DrawLine(CurrentLine.transform.TransformPoint(b2.c0),
                CurrentLine.transform.TransformPoint(b2.c0) + Vector3.up * 1.75f, Color.blue, 0.25f);
            Debug.DrawLine(CurrentLine.transform.TransformPoint(b2.c1),
                CurrentLine.transform.TransformPoint(b2.c1) + Vector3.up * 1.5f, Color.red, 0.25f);
            Debug.DrawLine(CurrentLine.transform.TransformPoint(b2.c2),
                CurrentLine.transform.TransformPoint(b2.c2) + Vector3.up * 1.25f, Color.blue, 0.25f);

            Debug.DrawLine(CurrentLine.transform.TransformPoint(b1.c1),
                CurrentLine.transform.TransformPoint(b2.c1), Color.green, 0.25f);
        }

        private void UpdateKnots()
        {
            float3 originPos = CurrentLine.OriginNode.transform.localPosition;
            float3 endPos = CurrentLine.EndNode.transform.localPosition;
            if (curve1.c0.IsCloseTo(curve2.c0))
            {
                CurrentLine.Line.SplineKnots = new float3[0];
                return;
            }

            switch (localMode)
            {
                case LocalToolMode.Straight:
                    CurrentLine.Line.SplineKnots = new[] { originPos, endPos };
                    return;
                case LocalToolMode.StraightOriginCurve:
                {
                    float3[] knots = GetPartCurveKnots(curve1);
                    if (knots.Length > 0 && knots[knots.Length - 1].IsCloseTo(endPos, 0.01f))
                    {
                        CurrentLine.Line.SplineKnots = new float3[knots.Length];
                    }
                    else
                    {
                        CurrentLine.Line.SplineKnots = new float3[knots.Length + 1];
                        CurrentLine.Line.SplineKnots[knots.Length] = endPos;
                    }

                    knots.CopyTo(CurrentLine.Line.SplineKnots, 0);
                    return;
                }
                case LocalToolMode.StraightEndCurve:
                {
                    float3[] knots = GetPartCurveKnots(curve1);
                    if (knots.Length > 0 && knots[0].IsCloseTo(originPos, 0.01f))
                    {
                        CurrentLine.Line.SplineKnots = new float3[knots.Length];
                        knots.CopyTo(CurrentLine.Line.SplineKnots, 0);
                    }
                    else
                    {
                        CurrentLine.Line.SplineKnots = new float3[knots.Length + 1];
                        CurrentLine.Line.SplineKnots[0] = originPos;
                        knots.CopyTo(CurrentLine.Line.SplineKnots, 1);
                    }

                    return;
                }
                case LocalToolMode.StraightOriginAndEndCurves:
                    float3[] knots1 = GetPartCurveKnots(curve1);
                    float3[] knots2 = GetPartCurveKnots(curve2, true);
                    if (knots1.Length > 0 && knots2.Length > 0 && knots1[knots1.Length-1].IsCloseTo(knots2[0]))
                    {
                        CurrentLine.Line.SplineKnots = new float3[knots1.Length + knots2.Length - 1];
                        knots2.CopyTo(CurrentLine.Line.SplineKnots, knots1.Length - 1);
                        knots1.CopyTo(CurrentLine.Line.SplineKnots, 0);
                    }
                    else
                    {
                        CurrentLine.Line.SplineKnots = new float3[knots1.Length + knots2.Length];
                        knots2.CopyTo(CurrentLine.Line.SplineKnots, knots1.Length);
                        knots1.CopyTo(CurrentLine.Line.SplineKnots, 0);
                    }
                    break;
                default:
                {
                    throw new NotImplementedException();
                }
            }
        }

        private float3[] GetPartCurveKnots(
            float3x3 bezier,
            bool invert = false)
        {
            float3 originPos = bezier.c0;
            float3 controlPoint = bezier.c1;
            float3 endPos = bezier.c2;

            if (controlPoint.Equals(originPos))
            {
                return new float3[0];
            }

            float lineDistance = math.distance(originPos, controlPoint) + math.distance(controlPoint, endPos);

            int numberOfKnots = (int) math.ceil(lineDistance / tool.KnotSpacing);
            float3[] knots = new float3[numberOfKnots];
            for (int i = 0; i < numberOfKnots; i++)
            {
                float t = (float) i / (numberOfKnots - 1);
                knots[i] = Bezier.Bezier.GetVectorOnCurve(originPos, controlPoint, endPos, invert ? 1f - t : t);
                float3 worldKnot = CurrentLine.transform.TransformPoint(knots[i]);
                Debug.DrawLine(worldKnot, worldKnot + new float3(0, 1, 0), Color.blue, 0.05f);
            }

            return knots;
        }

        private void AdjustLinePosition()
        {
            // Preserve Origin and end node positions
            Transform originTx = CurrentLine.OriginNode.transform;
            Transform endTx = CurrentLine.EndNode.transform;
            Vector3 originPos = originTx.position;
            Vector3 endPos = endTx.position;
            Quaternion originRotation = originTx.rotation;
            Quaternion endRotation = endTx.rotation;

            Vector3 midWayPoint = Vector3.Lerp(
                CurrentLine.OriginNode.transform.position,
                CurrentLine.EndNode.transform.position, 0.5f);
            CurrentLine.transform.position = midWayPoint;
            CurrentLine.transform.LookAt(endPos);

            // Restore positions
            originTx.position = originPos;
            endTx.position = endPos;

            // If origin is snapped, restore rotation
            if (originSnappedToNode)
            {
                originTx.rotation = originRotation;
            }

            if (endSnappedToNode)
            {
                endTx.rotation = endRotation;
            }
        }

        private void MoveEndNode()
        {
            Transform endTx = CurrentLine.EndNode.transform;
            if (endSnappedToNode)
            {
                Transform endSnappedToTx = endSnappedToNode.transform;
                endTx.position = endSnappedToTx.position;
                endTx.rotation = Quaternion.LookRotation(-endSnappedToNode.transform.forward);
                return;
            }

            endTx.position = CurrentLine.Cursor.transform.position;
        }

        public void EndLine()
        {
            CurrentLine.Line.NodeCollidersEnabled = true;
            ResetTool();
        }

        public void Cancel()
        {
            if (CurrentLine)
            {
                Object.Destroy(CurrentLine.gameObject);
            }
        }
    }
}