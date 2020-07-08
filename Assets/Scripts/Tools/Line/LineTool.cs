﻿using System;
using Unity.Mathematics;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Sibz.Lines
{
    public class LineTool
    {
        public LineBehaviour CurrentLine;

        private const float DefaultOriginDistance = 4f;
        private const float DefaultEndDistance = 4f;

        private readonly GameObject linePrefab;
        private readonly GameObject cursor;
        private readonly LineToolBehaviour tool;

        private float3x4 curve;

        private readonly SnapNotifierBehaviour snapNotifier;
        private GameObject originSnappedToNode;
        private GameObject endSnappedToNode;

        private MeshRenderer meshRenderer;

        public LineToolMode Mode { get; set; }

        /*private float originDistance;
        private float OriginDistance
        {
            get
            {
                if (!CurrentLine)
                {
                    return 0f;
                }

                float minLen = tool.MinCurveLength;
                float curveLen = CurrentLine.Line.CurveLength;//ProjectedCurveLengthOnXAndZAxisOnly;
                //float scale = 1 + math.abs(math.dot(CurrentLine.OriginNode.transform.forward,
                   // CurrentLine.OriginNode.transform.right));
                   bool condition = originDistance * curveLen > curveLen - minLen;
                   if (condition)
                   {
                       Debug.Log("originDistance was too long");
                   }


                return math.max(condition
                    ? (curveLen - minLen) / curveLen
                    : originDistance, minLen / curveLen);// * scale;
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
                float curveLen = ProjectedCurveLengthOnXAndZAxisOnly;
                bool condition = endDistance * curveLen > curveLen - minLen;
                if (condition)
                {
                    Debug.Log("endDistance was too long");
                }
                return math.max(condition
                    ? (curveLen - minLen) / curveLen
                    : endDistance, minLen / curveLen);
            }
            set => endDistance = value;
        }*/
        public float EndDistance { get; set; } = DefaultOriginDistance;
        public float OriginDistance { get; set; } = DefaultEndDistance;

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

            float minLengthPercent = tool.MinCurveLength / CurrentLine.Line.CurveLength;
            switch (nodeType)
            {
                case NodeType.Origin:
                    OriginDistance = math.clamp(OriginDistance + adjustment, 0, CurrentLine.Line.Length);
                    //math.clamp(OriginDistance + adjustment, tool.MinCurveLength,
                    //CurrentLine.Line.CurveLength);

                    break;
                case NodeType.End:
                    EndDistance = math.clamp(EndDistance + adjustment, 0, CurrentLine.Line.Length);

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
            /*UpdateKnots();
            AdjustEndNodeRotations();
            CurrentLine.Line.RebuildMesh();*/
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
            float3 lineEndA = originTx.localPosition;
            float3 lineEndB = endTx.localPosition;

            // Two curves  b1, b2
            float3x3 b1, b2;

            // c0 is the origin
            b1.c0 = lineEndA;
            b2.c0 = lineEndB;

            float l = math.distance(lineEndA, lineEndB);

            float3x2 forwards = new float3x2(
                CurrentLine.transform.InverseTransformDirection(originTx.forward),
                CurrentLine.transform.InverseTransformDirection(endTx.forward));
            ;
            float2 distances = new float2(
                Distance(lineEndA, lineEndB, forwards.c0),
                Distance(lineEndB, lineEndA, forwards.c1));
            float2 scales = new float2(
                Scale(OriginDistance, distances.x, forwards.c0, lineEndA, lineEndB),
                Scale(EndDistance, distances.y, forwards.c1, lineEndB, lineEndA));
            ;

            if (originSnappedToNode && !endSnappedToNode)
            {
                //originTx.rotation = originSnappedToNode.transform.forward;
                b1.c1 = GetOrigin();
                // Do Rotate
                endTx.LookAt(CurrentLine.transform.TransformPoint(b1.c1));
                b2.c1 = GetEnd();
            }
            else if (!originSnappedToNode && endSnappedToNode)
            {
                b2.c1 = GetEnd();
                // Do Rotate
                originTx.LookAt(b2.c1);
                b1.c1 = GetOrigin();
            }
            else
            {
                if (!originSnappedToNode && !endSnappedToNode)
                {
                    originTx.LookAt(endTx.position);
                    endTx.LookAt(originTx.position);
                }

                b1.c1 = GetOrigin();
                b2.c1 = GetEnd();
            }

            b1.c2 = Target(b1.c1, b2.c1, distances.x, distances.y, scales.x, scales.y);
            b2.c2 = Target(b2.c1, b1.c1, distances.y, distances.x, scales.y, scales.x);

            //Debug.Log(b1 + "\n" +  b2);

            float3 GetOrigin() => GetControlPoint(forwards.c0, lineEndA, distances.x, scales.x);
            float3 GetEnd() => GetControlPoint(forwards.c1, lineEndB, distances.y, scales.y);

            static float3 GetControlPoint(float3 f, float3 p1, float h, float s) => p1 + f * h * s;

            static float3 Target(float3 c1, float3 c2, float h1, float h2, float s1, float s2)
            {
                float d = math.distance(c1, c2);
                float ht = h1 * s1 + h2 * s2;
                return c1 + math.normalize(c2 - c1) * (ht > d ? d * (h1 * s1 / ht) : h1 * s1);
            }

            //static float Distance(float3 v, float3 f, float l)
            static float Distance(float3 p1, float3 p2, float3 forwards)
            {
                /*float angle = LineHelpers.AngleDegrees(v, f) / 2;
                angle = angle > 45 ? 45 + (angle - 45) / 1.1f : angle;
                angle = math.PI / 180 * angle;
                return l / 4 * math.cos(angle);*/
                float angle = AngleD(forwards, math.normalize(p2 - p1));
                angle = angle > 90 ? 90 + (angle - 90) / 2f : angle;

                float b = math.abs((math.distance(p2,p1) / SinD(180 - 2 * angle)) * SinD(angle));
                return b;
            }

            static float Dist(float3 a, float3 b) => math.distance(a,b);
            static float CosD(float a) => math.cos(math.PI / 180 * a);
            static float SinD(float a) => math.sin(math.PI / 180 * a);
            static float AngleD(float3 a, float3 b) => LineHelpers.AngleDegrees(a, b);
            static float Scale(float units, float distance, float3 forwards, float3 p1, float3 p2)
            {

                float angle = AngleD(forwards, math.normalize(p2 - p1));

                float b = math.abs((math.distance(p2,p1) / SinD(180 - 2 * angle)) * SinD(angle));
                Debug.Log(angle + " : " + b);
                /*float max1 = 2 * distances.x;
                float c = Dist(p1, p2);
                float b = distances.x;
                float a = math.sqrt(b * b + c * c - 2 * b * c * CosD(AngleD(forwards.c0, math.normalize(p2-p1))));
                float max2 = (distances.x + a) / 2;
                //float mod = 2 * (1 + math.pow(angle,math.PI)/math.pow(180,3));
                float thisScale = math.min(units / distances.x, math.max(max1, max2) / distances.x);
                //float otherScale = math.min(otherUnits / distances.y, mod);
                // Debug.Log(units + ":" + otherUnits + " d:" + distances + "\n" +
                //           thisScale + ":" + otherScale);
                /*if (thisScale + otherScale > 2)
                {
                    return 2 / (thisScale + otherScale) * thisScale / (otherScale + thisScale);
                }#1#

                return thisScale;*/
                return math.min(units / distance, 1);
            }

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

        private float3 GetControlPoint(float distance, Transform originTx)
        {
            float3 originPos = originTx.localPosition;
            /*float3 destPos = destTx.localPosition;
            float len = CurrentLine.Line.Length;
            float3 direction = destPos - originPos;*/
            float3 forward = CurrentLine.transform.InverseTransformDirection(originTx.forward);
            /*float3 destForward = CurrentLine.transform.InverseTransformDirection(destTx.forward);
            /*float angle1 = LineHelpers.AngleDegrees(direction, forward);
            float mod1 = angle1 / 180 * 2 + 1;
            float angle2 = LineHelpers.AngleDegrees(destForward, forward);
            float mod2 = angle2 / 180;#1#
            float x = len * (distance / 2);//* (mod1 - mod2);//(math.abs(math.dot(direction, forward)) / 10);*/
            return originPos + forward * (distance / 2);
        }

        private void UpdateKnots()
        {
            float3 knotStart = CurrentLine.OriginNode.transform.localPosition;
            float3 knotEnd = CurrentLine.EndNode.transform.localPosition;
            float curveLen = ProjectedCurveLengthOnXAndZAxisOnly; //CurrentLine.Line.CurveLength;//
            float originDist = OriginDistance * curveLen;
            float endDist = EndDistance * curveLen;
            switch (localMode)
            {
                case LocalToolMode.StraightOriginCurve:
                case LocalToolMode.StraightEndCurve:
                {
                    bool origin = localMode == LocalToolMode.StraightOriginCurve;
                    float3 controlPoint = origin
                        ? curve.c1
                        : curve.c2;
                    float3[] knots = origin
                        ? GetPartCurveKnots(originDist, CurrentLine.OriginNode.transform.localPosition,
                            controlPoint, knotEnd)
                        : GetPartCurveKnots(endDist, CurrentLine.EndNode.transform.localPosition,
                            controlPoint, knotStart, true);
                    CurrentLine.Line.SplineKnots = new float3[knots.Length + 1];
                    knots.CopyTo(CurrentLine.Line.SplineKnots, origin ? 0 : 1);
                    CurrentLine.Line.SplineKnots[origin ? knots.Length : 0] = origin ? knotEnd : knotStart;

                    break;
                }
                case LocalToolMode.StraightOriginAndEndCurves:
                {
                    float3[] knots1 = GetPartCurveKnots(originDist, CurrentLine.OriginNode.transform.localPosition,
                        curve.c1, curve.c2);
                    float3[] knots2 = GetPartCurveKnots(endDist, CurrentLine.EndNode.transform.localPosition,
                        curve.c2, curve.c1, true);
                    CurrentLine.Line.SplineKnots = new float3[knots1.Length + knots2.Length];
                    knots1.CopyTo(CurrentLine.Line.SplineKnots, 0);
                    knots2.CopyTo(CurrentLine.Line.SplineKnots, knots1.Length);
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

                    float3[] splineKnots = new float3 [numberOfKnots];

                    for (int i = 0; i < numberOfKnots; i++)
                    {
                        float t = (float) i / (numberOfKnots - 1);
                        float3 knot = Bezier.GetVectorOnCurve(curve, t);
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

        private float3[] GetPartCurveKnots(
            float distance,
            float3 originPos,
            float3 controlPoint,
            float3 target,
            bool invert = false)
        {
            float3 endPos = controlPoint +
                            math.normalize(target - controlPoint) * math.distance(controlPoint, originPos);

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

                float3 localCursorPosition = CurrentLine.transform.InverseTransformPoint(tool.transform.position);

                localCursorPosition.y = 0;
                float3 localOriginNodePosition = CurrentLine.OriginNode.transform.localPosition;

                int len = CurrentLine.Line.SplineKnots.Length;
                if (len < 2)
                {
                    return (projectedCurveLengthOnXAndZAxisOnlyCache =
                        math.distance(localOriginNodePosition, localCursorPosition)).Value;
                }

                float3[] splineKnots = new float3[len];
                CurrentLine.Line.SplineKnots.CopyTo(splineKnots, 0);
                if (localCursorPosition.IsCloseTo(splineKnots[len - 1], tool.KnotSpacing / 2))
                {
                    return (projectedCurveLengthOnXAndZAxisOnlyCache = CurrentLine.Line.CurveLength).Value;
                }

                float distance = 0;
                for (int i = 1; i < len; i++)
                {
                    splineKnots[i].y = 0;
                    float3 knotCurrent = splineKnots[i];
                    float3 knotPrevious = splineKnots[i - 1];

                    float3 currentForward = math.normalize(knotCurrent - knotPrevious);
                    float3 cursorForward = math.normalize(localCursorPosition - knotCurrent);
                    float ad = LineHelpers.AngleDegrees(currentForward, cursorForward);
                    if (ad > 90f)
                    {
                        Debug.Log("Angle > 90");
                        distance += math.distance(knotPrevious, localCursorPosition);
                        break;
                    }

                    distance += math.distance(knotCurrent, knotPrevious);
                    /*if (i == len - 1)
                    {
                        distance += math.distance(knotCurrent, localCursorPosition);
                    }*/
                }

                return (projectedCurveLengthOnXAndZAxisOnlyCache = distance).Value;
            }
        }
    }
}