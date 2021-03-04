using System.Collections.Generic;
using UnityEngine;

public static class MathUtility {
    public static Vector3 BezierPoint(List<Vector3> points, float t) {
        return (
            Mathf.Pow(1 - t, 3) * points[0] +
            3 * Mathf.Pow(1 - t, 2) * t * points[1] +
            3 * (1 - t) * Mathf.Pow(t, 2) * points[2] +
            Mathf.Pow(t, 3) * points[3]
        );
    }
}
