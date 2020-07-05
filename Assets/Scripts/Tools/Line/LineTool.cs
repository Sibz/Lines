using System;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Sibz.Lines
{
    public class LineTool
    {
        public LineBehaviour CurrentLine;

        const float DefaultOriginDistance = 0.5f;
        const float DefaultEndDistance = 0.5f;

        private readonly GameObject linePrefab;
        private readonly GameObject cursor;
        private readonly LineToolBehaviour tool;

        private float3x4 curve;

        private readonly SnapNotifierBehaviour snapNotifier;
        private GameObject originSnappedToNode;
        private GameObject endSnappedToNode;

        private MeshRenderer meshRenderer;

        public LineToolMode Mode { get; set; }

        private float originDistance;
        private float OriginDistance
        {
            get
            {
                if (!CurrentLine)
                {
                    return 0f;
                }

                float minLen = tool.MinCurveLength;
                float curveLen =  ProjectedCurveLengthOnXAndZAxisOnly; //CurrentLine.Line.CurveLength;//
                return math.max(originDistance * curveLen > curveLen - minLen
                    ? (curveLen - minLen) / curveLen
                    : originDistance, minLen / curveLen);
            }
            set => originDistance = value;
        }

        private float endDistance;
        private float EndDistance
        {
            get
            {
                if (!CurrentLine)
                {
                    return 0f;
                }
                float minLen = tool.MinCurveLength;
                float curveLen = ProjectedCurveLengthOnXAndZAxisOnly;//CurrentLine.Line.CurveLength;//
                return math.max(endDistance * curveLen > curveLen - minLen
                    ? (curveLen - minLen) / curveLen
                    : endDistance, minLen / curveLen);
            }
            set => endDistance = value;
        }

        private LocalToolMode localMode;

        private enum LocalToolMode
        {
            Straight,
            StraightOriginCurve,
            StraightEndCurve,
            StraightOriginAndEndCurves,
            CubicBezier,
            Bezier
        }

        public void ResetTool()
        {
            CurrentLine = null;
            OriginDistance = DefaultOriginDistance;
            EndDistance = DefaultEndDistance;
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

        public LineTool(GameObject cursor, GameObject linePrefab, LineToolBehaviour tool)
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
                    ? Quaternion.LookRotation(-originSnappedToNode.transform.forward)
                    : rotation;

            CurrentLine.Line.NodeCollidersEnabled = false;
        }

        public void AdjustDistance(NodeType nodeType, float adjustment)
        {
            if (!CurrentLine)
            {
                return;
            }

            float minLengthPercent = tool.MinCurveLength / CurrentLine.Line.CurveLength;
            switch (nodeType)
            {
                case NodeType.Origin:
                    OriginDistance = math.clamp(OriginDistance + adjustment, minLengthPercent,
                        1 - minLengthPercent);
                    if (OriginDistance + EndDistance > 1)
                    {
                        EndDistance = 1 - OriginDistance;
                    }

                    break;
                case NodeType.End:
                    EndDistance = math.clamp(EndDistance + adjustment, minLengthPercent,
                        1 - minLengthPercent);
                    if (EndDistance + OriginDistance > 1)
                    {
                        OriginDistance = 1 - EndDistance;
                    }

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
            projectedCurveLengthOnXAndZAxisOnlyCache = null;
            AdjustEndNodeRotations();
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
            bool isTooShortToCurve = CurrentLine.Line.Length < tool.MinCurvedLineLength;

            LocalToolMode StraightOrCurved() =>
                originSnappedToNode && endSnappedToNode
                    ? LocalToolMode.StraightOriginAndEndCurves
                    : originSnappedToNode
                        ? LocalToolMode.StraightOriginCurve
                        : endSnappedToNode
                            ? LocalToolMode.StraightEndCurve
                            : LocalToolMode.Straight;

            localMode = Mode switch
            {
                LineToolMode.CubicBezier => isTooShortToCurve
                    ? LocalToolMode.Straight
                    : LocalToolMode.CubicBezier,
                // TODO If ends are more than 180' different then use Cubic
                LineToolMode.Curve => isTooShortToCurve
                    ? LocalToolMode.Straight
                    : LocalToolMode.Bezier,
                LineToolMode.Straight => isTooShortToCurve
                    ? LocalToolMode.Straight
                    : StraightOrCurved(),
                var _ => throw new NotImplementedException()
            };
        }

        private void UpdateCurve()
        {
            Transform originTx = CurrentLine.OriginNode.transform;
            Transform endTx = CurrentLine.EndNode.transform;
            float3 originPoint = originTx.localPosition;
            float3 endPoint = endTx.localPosition;
            float curveLen = ProjectedCurveLengthOnXAndZAxisOnly; //CurrentLine.Line.CurveLength;//

            float3 controlPoint1 = default, controlPoint2;
            switch (localMode)
            {
                case LocalToolMode.CubicBezier:
                    //controlPoint1 = originTx.localPosition + CurrentLine.transform.InverseTransformDirection(originTx.forward) * (curveLen * OriginDistance);
                    //controlPoint2 = endTx.localPosition + CurrentLine.transform.InverseTransformDirection(endTx.forward) * (curveLen * EndDistance);
                    //break;
                case LocalToolMode.StraightOriginAndEndCurves:
                case LocalToolMode.StraightOriginCurve:
                case LocalToolMode.StraightEndCurve:
                    controlPoint1 = GetControlPoint(OriginDistance*curveLen, CurrentLine.OriginNode.transform);
                    controlPoint2 = GetControlPoint(EndDistance*curveLen, CurrentLine.EndNode.transform);
                    break;
                case LocalToolMode.Bezier:
                    controlPoint1 = GetControlPoint(OriginDistance*curveLen, CurrentLine.OriginNode.transform);
                    controlPoint2 = GetControlPoint(OriginDistance*curveLen, CurrentLine.EndNode.transform);
                    /*if (!LineHelpers.TryGetTransformPathIntersection2D(
                        CurrentLine.OriginNode.transform,
                        CurrentLine.EndNode.transform,
                        out Vector3 intersection
                        ))
                    {
                        controlPoint1 = originTx.localPosition + CurrentLine.transform.InverseTransformDirection(originTx.forward) * (curveLen * OriginDistance);
                        controlPoint2 = endTx.localPosition + CurrentLine.transform.InverseTransformDirection(endTx.forward) * (curveLen * OriginDistance);
                        break;
                    }

                    if (originTx.right.GetSideOfLineXZ(originTx.InverseTransformPoint(intersection)) != LineHelpers.SideOfLine.Left)
                    {
                        localMode = LocalToolMode.CubicBezier;
                        goto case LocalToolMode.CubicBezier;
                    }
                    controlPoint1 = new float3(intersection.x, math.lerp(originPoint.y, controlPoint1.y, 0.5f), intersection.z);
                    controlPoint2 = controlPoint1;*/
                    break;
                case LocalToolMode.Straight:
                    controlPoint1 = originPoint;
                    controlPoint2 = endPoint;
                    break;
                default:
                    throw new NotImplementedException();
            }

            curve = new float3x4(
                originPoint,
                controlPoint1,
                controlPoint2,
                endPoint);

            Debug.DrawLine(CurrentLine.transform.TransformPoint(curve.c1),
                CurrentLine.transform.TransformPoint(curve.c1) + Vector3.up, Color.red, 0.25f);
            Debug.DrawLine(CurrentLine.transform.TransformPoint(curve.c2),
                CurrentLine.transform.TransformPoint(curve.c2) + Vector3.up, Color.red, 0.25f);


        }

        private void AdjustEndNodeRotations()
        {
            int knotsLength = CurrentLine.Line.SplineKnots.Length;
            Transform endNodeTx = CurrentLine.EndNode.transform;
            Transform originNodeTx = CurrentLine.OriginNode.transform;
            Vector3 originNodePos = originNodeTx.position;
            Vector3 endNodePos = endNodeTx.position;
            if (!endSnappedToNode)
            {
                endNodeTx.LookAt(knotsLength < 2
                    ? originNodePos
                    : CurrentLine.transform.TransformPoint(curve.c1));
            }

            Debug.DrawLine(
                endNodePos,
                endNodePos + endNodeTx.forward);

            if (!originSnappedToNode)
            {
                Vector3 target = CurrentLine.Line.SplineKnots.Length > 1
                    ? (Vector3) CurrentLine.Line.SplineKnots[1]
                    : endNodePos;
                originNodeTx.LookAt(CurrentLine.transform.TransformPoint(target));
            }

            Debug.DrawLine(
                originNodePos,
                originNodePos + CurrentLine.OriginNode.transform.forward);
        }

        private void UpdateKnots()
        {
            float3 knotStart = CurrentLine.OriginNode.transform.localPosition;
            float3 knotEnd = CurrentLine.EndNode.transform.localPosition;
            float curveLen =  ProjectedCurveLengthOnXAndZAxisOnly;//CurrentLine.Line.Length;//
            float originDist = OriginDistance * curveLen;
            float endDist = EndDistance * curveLen;
            switch (localMode)
            {
                /*{
                    var controlPoint = GetControlPoint(OriginDistance, CurrentLine.OriginNode.transform,
                        out float adjustedDistance);
                    var knots = GetPartCurveKnots(adjustedDistance, CurrentLine.OriginNode.transform.localPosition,
                        controlPoint, knotEnd);
                    CurrentLine.Line.SplineKnots = new float3[knots.Length + 1];
                    knots.CopyTo(CurrentLine.Line.SplineKnots, 0);
                    CurrentLine.Line.SplineKnots[knots.Length] = knotEnd;
                    Debug.DrawLine(CurrentLine.transform.TransformPoint(knotEnd),
                        CurrentLine.transform.TransformPoint(knotEnd) + Vector3.up, Color.cyan, 0.25f);
                    return;
                }*/
                case LocalToolMode.StraightOriginCurve:
                case LocalToolMode.StraightEndCurve:
                {
                    bool origin = localMode == LocalToolMode.StraightOriginCurve;
                    //float adjustedDistance;
                    var controlPoint = origin
                        ? curve.c1
                        : curve.c2;
                    var knots = origin
                        ? GetPartCurveKnots(originDist, CurrentLine.OriginNode.transform.localPosition,
                            controlPoint, knotEnd)
                        : GetPartCurveKnots(endDist, CurrentLine.EndNode.transform.localPosition,
                            controlPoint, knotStart, true);
                    CurrentLine.Line.SplineKnots = new float3[knots.Length + 1];
                    knots.CopyTo(CurrentLine.Line.SplineKnots, origin ? 0 : 1);
                    CurrentLine.Line.SplineKnots[origin ? knots.Length : 0] = origin ? knotEnd : knotStart;
                    Debug.DrawLine(CurrentLine.transform.TransformPoint(knotStart),
                        CurrentLine.transform.TransformPoint(knotStart) + Vector3.up, Color.cyan, 0.25f);
                    Debug.DrawLine(CurrentLine.transform.TransformPoint(knotEnd),
                        CurrentLine.transform.TransformPoint(knotEnd) + Vector3.up, Color.cyan, 0.25f);
                    break;
                }
                case LocalToolMode.StraightOriginAndEndCurves:
                {
                    var controlPoint1 = GetControlPoint(originDist, CurrentLine.OriginNode.transform);
                    var controlPoint2 = GetControlPoint(endDist, CurrentLine.EndNode.transform);
                    var knots1 = GetPartCurveKnots(originDist, CurrentLine.OriginNode.transform.localPosition,
                        curve.c1, curve.c2);
                    var knots2 = GetPartCurveKnots(endDist, CurrentLine.EndNode.transform.localPosition,
                        curve.c2, curve.c1, true);
                    CurrentLine.Line.SplineKnots = new float3[knots1.Length + knots2.Length];
                    knots1.CopyTo(CurrentLine.Line.SplineKnots, 0);
                    knots2.CopyTo(CurrentLine.Line.SplineKnots, knots1.Length);
                    Debug.DrawLine(CurrentLine.transform.TransformPoint(knotEnd),
                        CurrentLine.transform.TransformPoint(knotEnd) + Vector3.up, Color.cyan, 0.25f);
                    Debug.DrawLine(CurrentLine.transform.TransformPoint(knotStart),
                        CurrentLine.transform.TransformPoint(knotStart) + Vector3.up, Color.cyan, 0.25f);
                    break;
                }
                case LocalToolMode.Straight:
                    CurrentLine.Line.SplineKnots = new float3[]
                    {
                        CurrentLine.OriginNode.transform.localPosition,
                        CurrentLine.EndNode.transform.localPosition
                    };
                    break;
                case LocalToolMode.Bezier:

                    goto case LocalToolMode.CubicBezier;
                case LocalToolMode.CubicBezier:
                    int numberOfKnots = (int) math.ceil(curveLen / tool.KnotSpacing) + 2;
                    ;
                    float3[] splineKnots = new float3 [numberOfKnots];



                    Debug.DrawLine(CurrentLine.transform.TransformPoint(knotStart),
                        CurrentLine.transform.TransformPoint(knotStart) + Vector3.up, Color.cyan, 0.25f);
                    Debug.DrawLine(CurrentLine.transform.TransformPoint(knotEnd),
                        CurrentLine.transform.TransformPoint(knotEnd) + Vector3.up, Color.cyan, 0.25f);

                    for (int i = 0; i < numberOfKnots; i++)
                    {
                        float t = (float) i / (numberOfKnots - 1);
                        float3 knot = localMode == LocalToolMode.Bezier
                            ? Bezier.GetVectorOnCurve(curve/*.c0, curve.c1, curve.c3*/, t)
                            : Bezier.GetVectorOnCurve(curve, t);
                        splineKnots[i] = knot;
                        Debug.DrawLine(CurrentLine.transform.TransformPoint(knot),
                            CurrentLine.transform.TransformPoint(knot) + Vector3.up, Color.yellow, 0.05f);
                    }

                    CurrentLine.Line.SplineKnots = splineKnots;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private float3 GetControlPoint(float distance, Transform originTx/*, out float adjustedDistance*/)
        {
            /*float lineP2PLength = CurrentLine.Line.CurveLength;
            adjustedDistance =
                localMode == LocalToolMode.StraightOriginAndEndCurves &&
                lineP2PLength < OriginDistance + EndDistance
                    ? distance / (OriginDistance + EndDistance) * lineP2PLength
                    : math.min(distance, lineP2PLength);
            return originTx.localPosition + originTx.forward * (adjustedDistance / 2f);
                    */
            return originTx.localPosition + CurrentLine.transform.InverseTransformDirection(originTx.forward) * (distance / 2f);
        }

        private float3[] GetPartCurveKnots(
            float distance,
            float3 originPos,
            float3 controlPoint,
            float3 target,
            bool invert = false)
        {
            float3 endPos = controlPoint + math.normalize(target - controlPoint) * distance / 2f;

            float3 worldOriginPos = CurrentLine.transform.TransformPoint(originPos);
            float3 worldControlPoint = CurrentLine.transform.TransformPoint(controlPoint);
            float3 worldEndPos = CurrentLine.transform.TransformPoint(endPos);

            Debug.DrawLine(worldOriginPos, worldOriginPos + new float3(0, 1, 0) * 2, Color.red, 0.05f);
            Debug.DrawLine(worldControlPoint, worldControlPoint + new float3(0, 1, 0) * 2, Color.red, 0.05f);
            Debug.DrawLine(worldEndPos, worldEndPos + new float3(0, 1, 0) * 2, Color.red, 0.05f);

            float lineDistance = math.distance(originPos, controlPoint) + math.distance(controlPoint, endPos);

            int numberOfKnots = (int) math.ceil(lineDistance / tool.KnotSpacing);
            float3[] knots = new float3[numberOfKnots];
            for (int i = 0; i < numberOfKnots; i++)
            {
                float t = (float) i / (numberOfKnots - 1);
                knots[i] = Bezier.GetVectorOnCurve(originPos, controlPoint, endPos, invert ? 1f - t : t);
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
            /*LineBehaviour line;
            if (
                OriginSnappedToNode &&
                OriginSnappedToNode.transform.parent &&
                (line = OriginSnappedToNode.transform.parent.GetComponent<LineBehaviour>()) &&
                (line.OriginNode == OriginSnappedToNode || line.EndNode == OriginSnappedToNode)
            )
            {
                line.MergeWith(CurrentLine as LineBehaviour, OriginSnappedToNode);
            }*/

            ResetTool();
        }

        public void Cancel()
        {
            if (CurrentLine)
            {
                Object.Destroy(CurrentLine.gameObject);
            }
        }

        public int projectedCurveLengthOnXAndZAxisOnlyCacheFrameCached;
        public float? projectedCurveLengthOnXAndZAxisOnlyCache;
        public float ProjectedCurveLengthOnXAndZAxisOnly
        {
            get
            {
                if (projectedCurveLengthOnXAndZAxisOnlyCache.HasValue
                    && projectedCurveLengthOnXAndZAxisOnlyCacheFrameCached == Time.frameCount)
                {
                    return projectedCurveLengthOnXAndZAxisOnlyCache.Value;
                }
                projectedCurveLengthOnXAndZAxisOnlyCacheFrameCached = Time.frameCount;

                float3 localCursorPosition = endSnappedToNode
                    ? endSnappedToNode.transform.localPosition
                    : CurrentLine.transform.InverseTransformPoint(CurrentLine.Cursor.transform.position);
                localCursorPosition.y = 0;
                float3 localOriginNodePosition = CurrentLine.OriginNode.transform.localPosition;

                int len = CurrentLine.Line.SplineKnots.Length;
                float3[] splineKnots = new float3[len];
                CurrentLine.Line.SplineKnots.CopyTo(splineKnots, 0);

                if (len < 2)
                {
                    return (projectedCurveLengthOnXAndZAxisOnlyCache = math.distance(localOriginNodePosition, localCursorPosition)).Value;
                }

                float distance = 0;
                for (int i = 1; i < len; i++)
                {
                    splineKnots[i].y = 0;
                    float3 knotCurrent = splineKnots[i];
                    float3 knotPrevious = splineKnots[i-1];

                    float3 currentForward = math.normalize(knotCurrent - knotPrevious);
                    float3 cursorForward = math.normalize(localCursorPosition - knotCurrent);
                    if (LineHelpers.AngleDegrees(currentForward,cursorForward) < 90)
                    {
                        distance += math.distance(knotCurrent, knotPrevious);
                    }
                    else if (i == len - 1)
                    {
                        distance += math.distance(knotCurrent, knotPrevious);
                        distance += math.distance(knotCurrent, localCursorPosition);
                    }
                    else
                    {
                        distance += math.distance(knotPrevious, localCursorPosition);
                        break;
                    }
                }

                return (projectedCurveLengthOnXAndZAxisOnlyCache = distance).Value;
            }
        }
    }
}