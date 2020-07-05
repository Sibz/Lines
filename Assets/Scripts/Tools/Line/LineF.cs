/*using System;
using System.Collections.Generic;
using UnityEngine;

public class LineF : IEquatable<LineF>
{
    public Vector2 StartPoint, EndPoint;
    public float Length => (EndPoint - StartPoint).magnitude;
    public double A, B, C;
    public LineF(float x, float y, float x2, float y2) : this(new Vector2(x, y), new Vector2(x2, y2)) { }
    public LineF(Vector2 startPoint, Vector2 endPoint)
    {
        StartPoint = startPoint;
        EndPoint = endPoint;
        A = EndPoint.y - StartPoint.y;
        B = StartPoint.x - EndPoint.x;
        C = (A * StartPoint.x) + (B * StartPoint.y);
    }
    private bool Intersect(LineF otherLine, out Vector2 intersect)
    {
        if (StartPoint == otherLine.StartPoint && EndPoint == otherLine.EndPoint)
        {
            intersect = Vector2.zero;
            Debug.Log("Intersect not found Lines are the same");
            return false;
        }

        double delta = A * otherLine.B -ot herLine.A * B;

        if (Math.Abs(delta) < double.Epsilon)
        {
            intersect = Vector2.zero;
            //Debug.Log("Intersect not found delta is close to 0");
            return false;
        }

        intersect = new Vector2(
            (float)((otherLine.B * C - B * otherLine.C) / delta),
            (float)((A * otherLine.C - otherLine.A * C) / delta)
        );
        return true;
        /*
       var t = otherLine.StartPoint.x * StartPoint.x * EndPoint.y - otherLine.StartPoint.y * StartPoint.y * EndPoint.x;
       if (Mathf.Approximately(t, 0f))
       {
           intersect = Vector2.zero;
           return false;
       }
       intersect = new Vector2(
         StartPoint.x + EndPoint.x * t,
         StartPoint.y + EndPoint.y * t
           );
       return true;
       #1#
    }

    public bool Intersects(LineF line, out Vector2 intersection, bool ignoreIfSharesEndPoints = false, bool applyEdgeRules = true)
    {
        if (ignoreIfSharesEndPoints && (StartPoint.IsApproximately(line.StartPoint) || StartPoint.IsApproximately(line.EndPoint)
            || EndPoint.IsApproximately(line.EndPoint) || EndPoint.IsApproximately(line.StartPoint)))
        {
            intersection = Vector2.zero;
            //Debug.Log("Intersect not found - ignoring end points");
            return false;

        }
        if (Intersect(line, out Vector2 intersect))
        {
            float aX1, aX2, aY1, aY2, bX1, bX2, bY1, bY2, iX, iY;
            /*
            const int roundTo = 5;
            aX1 = Math.Round(StartPoint.x, roundTo);
            aX2 = Math.Round(EndPoint.x, roundTo);
            aY1 = Math.Round(StartPoint.y, roundTo);
            aY2 = Math.Round(EndPoint.y, roundTo);
            bX1 = Math.Round(line.StartPoint.x, roundTo);
            bX2 = Math.Round(line.EndPoint.x, roundTo);
            bY1 = Math.Round(line.StartPoint.y, roundTo);
            bY2 = Math.Round(line.EndPoint.y, roundTo);
            iX = Math.Round(intersect.x, roundTo);
            iY = Math.Round(intersect.y, roundTo);
            #1#
            aX1 = StartPoint.x;
            aX2 = EndPoint.x;
            aY1 = StartPoint.y;
            aY2 = EndPoint.y;
            bX1 = line.StartPoint.x;
            bX2 = line.EndPoint.x;
            bY1 = line.StartPoint.y;
            bY2 = line.EndPoint.y;
            iX = intersect.x;
            iY = intersect.y;

            // Debug.Log(string.Format("Line intesects x:{0} y:{1}",iX,iY));

            if (!ignoreIfSharesEndPoints)
            {
                if (applyEdgeRules)
                {
                    if (
                    (iX > Mathf.Min(aX1, aX2) || Mathf.Approximately(iX, Mathf.Min(aX1, aX2))) && (iX < Mathf.Max(aX1, aX2) || Mathf.Approximately(iX, Mathf.Max(aX1, aX2))) &&
                    (iY > Mathf.Min(aY1, aY2) || Mathf.Approximately(iY, Mathf.Min(aY1, aY2))) && (iY < Mathf.Max(aY1, aY2) || Mathf.Approximately(iY, Mathf.Max(aY1, aY2)))
                    &&
                    // Rule 1 upward edge includes start but not end
                    ((line.StartPoint.x < line.EndPoint.x) ?
                    (iX > Mathf.Min(bX1, bX2) || Mathf.Approximately(iX, Mathf.Min(bX1, bX2))) && iX < Mathf.Max(bX1, bX2) &&
                    (iY > Mathf.Min(bY1, bY2) || Mathf.Approximately(iY, Mathf.Min(bY1, bY2))) && iY < Mathf.Max(bY1, bY2) :
                    // Rule 2 downward edge includes end but not start
                    iX > Mathf.Min(bX1, bX2) && (iX < Mathf.Max(bX1, bX2) || Mathf.Approximately(iX, Mathf.Min(bX1, bX2))) &&
                    iY > Mathf.Min(bY1, bY2) && (iY < Mathf.Max(bY1, bY2) || Mathf.Approximately(iY, Mathf.Min(bY1, bY2))))
                )
                    {
                        intersection = intersect;
                        return true;
                    }
                    //Debug.Log("Intersect not found - did not fall on line (applyEdgeRules)");
                }
                else if ((iX > Mathf.Min(bX1, bX2) || Mathf.Approximately(iX, Mathf.Min(bX1, bX2))) && (iX < Mathf.Max(bX1, bX2) || Mathf.Approximately(iX, Mathf.Max(bX1, bX2))) &&
                   (iY > Mathf.Min(bY1, bY2) || Mathf.Approximately(iY, Mathf.Min(bY1, bY2))) && (iY < Mathf.Max(bY1, bY2) || Mathf.Approximately(iY, Mathf.Max(bY1, bY2)))
                   &&
                   (iX > Mathf.Min(aX1, aX2) || Mathf.Approximately(iX, Mathf.Min(aX1, aX2))) && (iX < Mathf.Max(aX1, aX2) || Mathf.Approximately(iX, Mathf.Max(aX1, aX2))) &&
                   (iY > Mathf.Min(aY1, aY2) || Mathf.Approximately(iY, Mathf.Min(aY1, aY2))) && (iY < Mathf.Max(aY1, aY2) || Mathf.Approximately(iY, Mathf.Max(aY1, aY2))))
                {
                    intersection = intersect;
                    return true;
                }
                else
                {
                    //Debug.Log("Intersect not found - did not fall on line (!applyEdgeRules)");
                }
            }
            else if ((iX > Mathf.Min(bX1, bX2)) && (iX < Mathf.Max(bX1, bX2)) &&
                    (iY > Mathf.Min(bY1, bY2)) && (iY < Mathf.Max(bY1, bY2))
                    &&
                    (iX > Mathf.Min(aX1, aX2)) && (iX < Mathf.Max(aX1, aX2)) &&
                    (iY > Mathf.Min(aY1, aY2)) && (iY < Mathf.Max(aY1, aY2)))
            {
                intersection = Vector2.zero;
                //Debug.Log("Intersect not found - ignoring end points2");
                return false;

            }
            /*
            #1#


        }
        intersection = Vector2.zero;
        return false;
    }
    public bool Intersects(LineF[] lines, bool ignoreIfSharesEndPoints = false, bool applyEdgeRules = true)
    {
        for (int i = 0; i < lines.Length; i++)
        {
            if (Intersects(lines[i], out Vector2 r, ignoreIfSharesEndPoints, applyEdgeRules)) return true;
        }
        return false;
    }
    /// <summary>
    /// Returns closest intersection of from lines
    /// </summary>
    /// <param name="lines"></param>
    /// <param name="intersection"></param>
    /// <param name="applyEdgeCrossingRules"></param>
    /// <returns></returns>
    public bool Intersects(LineF[] lines, out Vector2 intersection, bool ignoreIfSharesEndPoints = false, bool applyEdgeRules = true)
    {
        List<Vector2> intersections = new List<Vector2>();
        for (int i = 0; i < lines.Length; i++)
        {
            if (Intersects(lines[i], out Vector2 r, ignoreIfSharesEndPoints, applyEdgeRules))
            {
                intersections.Add(r);
            }
        }
        if (intersections.Count > 0)
        {
            Vector2 closestIntersection = intersections[0];
            foreach (var interx in intersections)
            {
                //
                if ((StartPoint - interx).magnitude < (StartPoint - closestIntersection).magnitude)
                    closestIntersection = interx;

            }
            intersection = closestIntersection;
            return true;
        }

        intersection = Vector2.zero;
        return false;
    }

    public bool SimpleIntersect(LineF[] lines, out Vector2 intersection)
    {
        List<Vector2> intersections = new List<Vector2>();
        for (int i = 0; i < lines.Length; i++)
        {
            var other = lines[i];
            if (
                    (
                        (StartPoint.GetSideOfLine(other.StartPoint) == VectorExtensions.SideOfLine.Left && (StartPoint.GetSideOfLine(other.EndPoint) != VectorExtensions.SideOfLine.Left))
                        ||
                        (StartPoint.GetSideOfLine(other.StartPoint) == VectorExtensions.SideOfLine.Right && (StartPoint.GetSideOfLine(other.EndPoint) != VectorExtensions.SideOfLine.Right))
                        ||
                         (other.StartPoint.GetSideOfLine(StartPoint) == VectorExtensions.SideOfLine.Right && (other.StartPoint.GetSideOfLine(EndPoint) != VectorExtensions.SideOfLine.Right))
                        ||
                        (other.StartPoint.GetSideOfLine(StartPoint) == VectorExtensions.SideOfLine.Left && (other.StartPoint.GetSideOfLine(EndPoint) != VectorExtensions.SideOfLine.Left))
                    )
                &&
                    (
                        (StartPoint.GetSideOfLine(other.StartPoint) == VectorExtensions.SideOfLine.Right && (StartPoint.GetSideOfLine(other.EndPoint) != VectorExtensions.SideOfLine.Right))
                        ||
                        (StartPoint.GetSideOfLine(other.StartPoint) == VectorExtensions.SideOfLine.Left && (StartPoint.GetSideOfLine(other.EndPoint) != VectorExtensions.SideOfLine.Left))
                        ||
                        (other.StartPoint.GetSideOfLine(StartPoint) == VectorExtensions.SideOfLine.Left && (other.StartPoint.GetSideOfLine(EndPoint) != VectorExtensions.SideOfLine.Left))
                        ||
                        (other.StartPoint.GetSideOfLine(StartPoint) == VectorExtensions.SideOfLine.Right && (other.StartPoint.GetSideOfLine(EndPoint) != VectorExtensions.SideOfLine.Right))
                    )
                )
            {
                Intersect(other, out intersection);
                intersections.Add(intersection);
            }
        }
        if (intersections.Count > 0)
        {
            if (intersections.Count > 1)
                Debug.LogFormat("SimpleIntersects had {0} or more intersects!", intersections.Count);
            Vector2 closestIntersection = intersections[0];
            foreach (var interx in intersections)
            {
                //
                if ((StartPoint - interx).magnitude  < (StartPoint - closestIntersection).magnitude)
                    closestIntersection = interx;

            }
            intersection = closestIntersection;
            return true;
        }
        intersection = Vector2.zero;
        return false;

        //return Intersects(lines);

    }
    public bool LineSegementsIntersect(LineF[] lines, out Vector2 intersection)
    {
        List<Vector2> intersections = new List<Vector2>();
        for (int i = 0; i < lines.Length; i++)
        {
            if (LineSegementsIntersect(lines[i], out Vector2 r))
            {
                intersections.Add(r);
            }
        }
        if (intersections.Count > 0)
        {
            Vector2 closestIntersection = intersections[0];
            foreach (var interx in intersections)
            {
                //
                if ((StartPoint - interx).magnitude + (EndPoint - interx).magnitude < (StartPoint - closestIntersection).magnitude + (EndPoint - closestIntersection).magnitude)
                    closestIntersection = interx;

            }
            intersection = closestIntersection;
            return true;
        }

        intersection = Vector2.zero;
        return false;
    }

    public bool LineSegementsIntersect(LineF otherLine,
    out Vector2 intersection, bool considerCollinearOverlapAsIntersect = false)
    {
        intersection = new Vector2();

        var p = StartPoint;
        var p2 = EndPoint;
        var q = otherLine.StartPoint;
        var q2 = otherLine.EndPoint;
        var r = p2 - p;
        var s = q2 - q;
        var rxs = r.x * s.x - r.y * s.x;
        var qpxr = (q - p).x * r.x - (q - p).y * r.x;
        var qpxs = (q - p).x * s.x - (q - p).y * s.x;

        // If r x s = 0 and (q - p) x r = 0, then the two lines are collinear.
        if (Mathf.Approximately(rxs, 0f) && Mathf.Approximately(qpxr, 0f))
        {
            // 1. If either  0 <= (q - p) * r <= r * r or 0 <= (p - q) * s <= * s
            // then the two lines are overlapping,
            //if (considerCollinearOverlapAsIntersect)
            //  if ((Vector2.zero <= (q - p) * r && (q - p) * r <= r * r) || (0 <= (p - q) * s && (p - q) * s <= s * s))
            //    return true;

            // 2. If neither 0 <= (q - p) * r = r * r nor 0 <= (p - q) * s <= s * s
            // then the two lines are collinear but disjoint.
            // No need to implement this expression, as it follows from the expression above.
            return false;
        }

        // 3. If r x s = 0 and (q - p) x r != 0, then the two lines are parallel and non-intersecting.
        if (Mathf.Approximately(rxs,0f) && !Mathf.Approximately(qpxr, 0f))
            return false;

        // t = (q - p) x s / (r x s)
        var t = qpxs / rxs;

        // u = (q - p) x r / (r x s)

        var u = qpxr / rxs;

        // 4. If r x s != 0 and 0 <= t <= 1 and 0 <= u <= 1
        // the two line segments meet at the point p + t r = q + u s.
        if (!Mathf.Approximately(rxs, 0f) && (0 <= t && t <= 1) && (0 <= u && u <= 1))
        {
            // We can calculate the intersection point using either t or u.
            intersection = p + t * r;

            // An intersection was found.
            return true;
        }

        // 5. Otherwise, the two line segments are not parallel but do not intersect.
        return false;
    }
    public bool Equals(LineF other)
    {
        return (StartPoint == other.StartPoint && EndPoint == other.EndPoint) || (StartPoint == other.EndPoint && EndPoint == other.StartPoint);
    }
}*/