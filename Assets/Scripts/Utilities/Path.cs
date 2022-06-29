using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Path
{
    [SerializeField, HideInInspector]
    List<Vector2> points;
    [SerializeField, HideInInspector]
    bool isClosed = false;
    [SerializeField, HideInInspector]
    bool autoSetControlPoints;

    public Path(Vector2 center)
    {
        points = new List<Vector2>
        {
            center + Vector2.left,
            center + (Vector2.left + Vector2.up) * 0.5f,
            center + (Vector2.right + Vector2.down) * 0.5f,
            center + Vector2.right,
        };
    }

    public Vector2 this[int i]
    {
        get
        {
            return points[i];
        }
    }

    public int PointCount
    {
        get
        {
            return points.Count;
        }
    }
    
    public int SegmentCount
    {
        get
        {
            return points.Count / 3;
        }
    }

    public bool IsClosed
    {
        get
        {
            return isClosed;
        }
        set
        {
            if (isClosed != value)
            {
                isClosed = value;
                if (isClosed)
                {
                    // add two control points to the first anchor and the last anchor
                    points.Add(points[points.Count - 1] * 2 - points[points.Count - 2]);
                    points.Add(points[0] * 2 - points[1]);

                    if (autoSetControlPoints)
                    {
                        AutoSetAnchorControlPoints(0);
                        AutoSetAnchorControlPoints(points.Count - 3);
                    }
                }
                else
                {
                    points.RemoveRange(points.Count - 2, 2);
                    if (autoSetControlPoints)
                    {
                        AutoSetStartAndEndControl();
                    }
                }
            }
        }
    }

    public bool AutoSetControlPoints
    {
        get
        {
            return autoSetControlPoints;
        }
        set
        {
            if (autoSetControlPoints != value)
            {
                autoSetControlPoints = value;
                if (autoSetControlPoints)
                {
                    AutoSetAllControlPoints();
                }
            }
        }
    }

    public void AddSegment(Vector2 anchor)
    {
        points.Add(points[points.Count - 1] * 2 - points[points.Count - 2]);
        points.Add((points[points.Count - 1] + anchor) * 0.5f);
        points.Add(anchor);

        if (autoSetControlPoints)
        {
            AutoSetAllAffectedControlPoints(points.Count - 1);
        }
    }

    public void DeleteSegment(int anchorIndex)
    {
        if (SegmentCount > 2 || (!isClosed && SegmentCount > 1))
        {
            if (anchorIndex == 0)
            {
                if (isClosed)
                {
                    points[points.Count - 1] = points[2];
                }
                points.RemoveRange(0, 3);
            }
            else if (anchorIndex == points.Count - 1 && !isClosed)
            {
                points.RemoveRange(anchorIndex - 2, 3);
            }
            else
            {
                points.RemoveRange(anchorIndex - 1, 3);
            }
        }
    }

    public void SplitSegment(Vector2 anchorPosition, int segmentIndex)
    {
        points.InsertRange(segmentIndex * 3 + 2, new Vector2[] { Vector2.zero, anchorPosition, Vector2.zero });
        
        if (AutoSetControlPoints)
        {
            AutoSetAllAffectedControlPoints(segmentIndex * 3 + 3);
        }
        else
        {
            AutoSetAnchorControlPoints(segmentIndex * 3 + 3);
        }
    }

    public Vector2[] CalculateEvenlySpacedPoints(float spacing, float resolution = 1)
    {
        List<Vector2> evenlySpacedPoints = new List<Vector2>();
        evenlySpacedPoints.Add(points[0]);

        Vector2 previousPoint = points[0];
        float distanceSinceLastEvenPoint = 0;

        for (int segmentIndex = 0; segmentIndex < SegmentCount; segmentIndex++)
        {
            Vector2[] p = GetPointsInSegments(segmentIndex);
            float controlNetLength = Vector2.Distance(p[0], p[1]) + Vector2.Distance(p[1], p[2]) + Vector2.Distance(p[2], p[3]);
            float estimateCurveLangth = Vector2.Distance(p[0], p[3]) + controlNetLength / 2f;
            int divisions = Mathf.CeilToInt(estimateCurveLangth * resolution * 10);
            float t = 0;
            while (t <= 1)
            {
                t += 1f / divisions;
                Vector2 pointOnCurve = Bazier.EvaluateCubic(p[0], p[1], p[2], p[3], t);
                distanceSinceLastEvenPoint += Vector2.Distance(previousPoint, pointOnCurve);

                while (distanceSinceLastEvenPoint >= spacing)
                {
                    float overshooDistance = distanceSinceLastEvenPoint - spacing;
                    Vector2 newEvenlySpacedPoint = pointOnCurve + (previousPoint - pointOnCurve).normalized * overshooDistance;
                    evenlySpacedPoints.Add(newEvenlySpacedPoint);
                    distanceSinceLastEvenPoint = overshooDistance;
                    previousPoint = newEvenlySpacedPoint;
                }

                previousPoint = pointOnCurve;
            }
        }
        return evenlySpacedPoints.ToArray();
    }

    public Vector2[] GetPointsInSegments(int i)
    {
        // anchor1, control1, control2, anchor2
        return new Vector2[] { points[i * 3], points[i * 3 + 1], points[i * 3 + 2], points[LoopIndex(i * 3 + 3)] };
    }

    public Vector2[] GetAnchorPoints()
    {
        List<Vector2> anchorPoints = new List<Vector2>();

        for (int i = 0; i < points.Count; i++)
        {
            if (i % 3 == 0)
            {
                anchorPoints.Add(points[i]);
            }
        }

        return anchorPoints.ToArray();
    }

    public void MovePoint(int i, Vector2 position)
    {
        // when autoSetControlPoints, cannot move control point
        if (autoSetControlPoints && i % 3 != 0) return; 

        Vector2 deltaMove = position - points[i];

        points[i] = position;

        if (autoSetControlPoints)
        {
            AutoSetAllAffectedControlPoints(i);
        }
        else
        {
            if (i % 3 == 0) // moving anchor point, control points should move along
            {
                if (i + 1 < points.Count || isClosed)
                {
                    points[LoopIndex(i + 1)] += deltaMove;
                }
                if (i - 1 >= 0 || isClosed)
                {
                    points[LoopIndex(i - 1)] += deltaMove;
                }
            }
            else // moving control point, the other control point rotate around the anchor
            {
                bool isPreviousPointAnchor = i - 1 >= 0 && i % 3 == 1;
                int anchorIndex = isPreviousPointAnchor ? i - 1 : i + 1;
                int otherControlPointIndex = isPreviousPointAnchor ? i - 2 : i + 2;

                if ((otherControlPointIndex >= 0 && otherControlPointIndex < points.Count) || isClosed)
                {
                    float distanceToAnchor = (points[LoopIndex(anchorIndex)] - points[LoopIndex(otherControlPointIndex)]).magnitude;
                    Vector2 direction = (points[LoopIndex(anchorIndex)] - position).normalized;

                    points[LoopIndex(otherControlPointIndex)] = points[LoopIndex(anchorIndex)] + direction * distanceToAnchor;
                }
            
            }
        }
    }

    private void AutoSetAllAffectedControlPoints(int updateAnchorIndex)
    {
        for (int i = updateAnchorIndex - 3; i <= updateAnchorIndex + 3; i+=3)
        {
            if ((i >= 0 && i < points.Count) || isClosed)
            {
                AutoSetAnchorControlPoints(LoopIndex(i));
            }
        }
        AutoSetStartAndEndControl();
    }

    private void AutoSetAllControlPoints()
    {
        for (int i = 0; i < points.Count; i+=3)
        {
            AutoSetAnchorControlPoints(i);
        }
        AutoSetStartAndEndControl();
    }

    private void AutoSetAnchorControlPoints(int anchorIndex)
    {
        Vector2 anchor = points[anchorIndex];
        Vector2 direction = Vector2.zero;
        float[] neighbourDistance = new float[2];

        if (anchorIndex - 3 >= 0 || isClosed)
        {
            Vector2 offset = points[LoopIndex(anchorIndex - 3)] - anchor;
            direction += offset.normalized;
            neighbourDistance[0] = offset.magnitude;
        }
        if (anchorIndex + 3 >= 0 || isClosed)
        {
            Vector2 offset = points[LoopIndex(anchorIndex + 3)] - anchor;
            direction -= offset.normalized;
            neighbourDistance[1] = -offset.magnitude;
        }

        direction.Normalize();

        for (int i = 0; i < 2; i++)
        {
            int controlIndex = anchorIndex + i * 2 - 1;
            if ((controlIndex >= 0 && controlIndex < points.Count) || isClosed)
            {
                points[LoopIndex(controlIndex)] = anchor + direction * neighbourDistance[i] * 0.5f;
            }
        }
    }

    private void AutoSetStartAndEndControl()
    {
        if (!isClosed)
        {
            points[1] = (points[0] + points[2]) * 0.5f;
            points[points.Count - 2] = (points[points.Count - 1] + points[points.Count - 3]) * 0.5f;
        }
    }

    private int LoopIndex(int i)
    {
        return (i + points.Count) % points.Count;
    }
}
