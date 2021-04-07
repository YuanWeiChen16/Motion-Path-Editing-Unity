using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public abstract class CurveFitBase
{
    protected const float EPSILON = Vector3.kEpsilon;
    protected const int MAX_ITERS = 4;
    protected const int END_TAGENT_N_PTS = 8;
    protected const int MID_TAGENT_N_PTS = 8;

    protected readonly List<Vector3> _pts = new List<Vector3>();

    protected readonly List<float> _arclen = new List<float>();

    protected readonly List<float> _u = new List<float>();

    protected float _squaredError;

    //protected bool FitCurve(int first, int last, Vector3 tanL, Vector3 tanR, out CubicBezier curve, out int split)
    //{
    //    List<Vector3> pts = _pts;
    //    int nPts = last - first + 1;
    //    if (nPts < 2)
    //    {
    //        throw new Exception("Need at least 2 points");
    //    }
    //    else if (nPts == 2)
    //    {
    //        Vector3 p0 = pts[first];
    //        Vector3 p3 = pts[last];
    //        float alpha = Vector3.Distance(p0, p3) / 3;
    //    }
        
    //}
}
