using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public sealed class CurveFit
{
    public static List<Vector3> DouglasPeuckerReduction(List<Vector3> points, float epsilon)
    {
        if (points == null || points.Count < 3)
            return points;

        int firstPoint = 0;
        int lastPoint = points.Count - 1;
        List<int> pointIndexKeeper = new List<int>();

        // add the first and last index to the keeper
        pointIndexKeeper.Add(firstPoint);
        pointIndexKeeper.Add(lastPoint);

        // the first and the last point cannot be the same
        while (points[firstPoint].Equals(points[lastPoint]))
        {
            lastPoint--;
        }

        DouglasPeuckerReduction(points, firstPoint, lastPoint, epsilon, ref pointIndexKeeper);

        List<Vector3> returnPoints = new List<Vector3>();
        pointIndexKeeper.Sort();
        foreach (int index in pointIndexKeeper)
        {
            returnPoints.Add(points[index]);
        }

        return returnPoints;
    }
    public static void DouglasPeuckerReduction(List<Vector3> points, int firstPoint, int lastPoint, float epsilon, ref List<int> pointIndexKeeper)
    {
        double maxDistance = 0;
        int indexFarthest = 0;

        for (int index = firstPoint; index < lastPoint; index++)
        {
            double distance = PerpendicularDistance(points[firstPoint], points[lastPoint], points[index]);
            if (distance > maxDistance)
            {
                maxDistance = distance;
                indexFarthest = index;
            }
        }

        if (maxDistance > epsilon && indexFarthest != 0)
        {
            // add the largest point that exceeds the tolerance
            pointIndexKeeper.Add(indexFarthest);

            DouglasPeuckerReduction(points, firstPoint, indexFarthest, epsilon, ref pointIndexKeeper);
            DouglasPeuckerReduction(points, indexFarthest, lastPoint, epsilon, ref pointIndexKeeper);
        }
    }
    public static double PerpendicularDistance(Vector3 point1, Vector3 point2, Vector3 point)
    {
        Vector3 d = (point2 - point1) / Vector3.Distance(point2, point1);
        Vector3 v = point - point1;
        float t = Vector3.Dot(v, d);
        Vector3 P = point1 + t * d;
        return Vector3.Distance(P, point);
    }

    public static CubicBezier[] Fit(List<Vector3> points, float maxError)
    {
        if (points == null)
            throw new Exception("points");
        if (points.Count < 2)
            return new CubicBezier[0];


        return new CubicBezier[2];
    }
}
