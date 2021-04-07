using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CubicBezier
{
    public readonly Vector3 p0;
    public readonly Vector3 p1;
    public readonly Vector3 p2;
    public readonly Vector3 p3;

    public CubicBezier(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
    {
        this.p0 = p0;
        this.p1 = p1;
        this.p2 = p2;
        this.p3 = p3;
    }

    public Vector3 Sample(float t)
    {
        float ti = 1 - t;
        float t0 = ti * ti * ti;
        float t1 = 3 * ti * ti * t;
        float t2 = 3 * ti * t * t;
        float t3 = t * t * t;
        return (t0 * p0) + (t1 * p1) + (t2 * p2) + (t3 * p3);
    }

    public Vector3 Derivative(float t)
    {
        float ti = 1 - t;
        float tp0 = 3 * ti * ti;
        float tp1 = 6 * ti * t;
        float tp2 = 3 * t * t;
        return (tp0 * (p1 - p0)) + (tp1 * (p2 - p1)) + (tp2 * (p3 - p2));
    }

    public Vector3 Tagent(float t)
    {
        return Vector3.Normalize(Derivative(t));
    }

    public static bool operator ==(CubicBezier left, CubicBezier right) { return left.Equals(right); }
    public static bool operator !=(CubicBezier left, CubicBezier right) { return !left.Equals(right); }
    public bool Equals(CubicBezier other) { return p0.Equals(other.p0) && p1.Equals(other.p1) && p2.Equals(other.p2) && p3.Equals(other.p3); }
    public override bool Equals(object obj) { return obj is CubicBezier && Equals((CubicBezier)obj); }
}
