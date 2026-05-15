using UnityEngine;

namespace Warblade.Systems
{
    /// <summary>
    /// Pure-math helpers for evaluating Bezier curves and approximating their arc length.
    /// Used for enemy entry paths and dive trajectories. No MonoBehaviour, no allocations.
    /// </summary>
    public static class BezierPath
    {
        /// <summary>
        /// Quadratic Bezier: B(t) = (1-t)^2 * p0 + 2(1-t)t * p1 + t^2 * p2.
        /// </summary>
        public static Vector2 EvaluateQuadratic(Vector2 p0, Vector2 p1, Vector2 p2, float t)
        {
            float oneMinusT = 1f - t;
            return oneMinusT * oneMinusT * p0
                 + 2f * oneMinusT * t * p1
                 + t * t * p2;
        }

        /// <summary>
        /// Approximates the arc length of a quadratic Bezier by sampling N segments
        /// and summing their straight-line distances. Higher samples = closer to true length.
        /// </summary>
        public static float ApproximateQuadraticLength(Vector2 p0, Vector2 p1, Vector2 p2, int samples = 16)
        {
            if (samples < 1) samples = 1;

            float length = 0f;
            Vector2 prev = p0;
            for (int i = 1; i <= samples; i++)
            {
                float t = i / (float)samples;
                Vector2 point = EvaluateQuadratic(p0, p1, p2, t);
                length += Vector2.Distance(prev, point);
                prev = point;
            }
            return length;
        }

        /// <summary>
        /// Evaluates a chain of quadratic Bezier segments. Midpoint controls produce straight lines.
        /// </summary>
        public static Vector2 EvaluateSegmentedQuadraticPath(Vector2[] points, Vector2[] controlPoints, float t)
        {
            if (points == null || points.Length == 0)
            {
                return Vector2.zero;
            }

            if (points.Length == 1)
            {
                return points[0];
            }

            if (controlPoints == null || controlPoints.Length == 0)
            {
                return Vector2.Lerp(points[0], points[points.Length - 1], Mathf.Clamp01(t));
            }

            float clampedT = Mathf.Clamp01(t);
            int segmentCount = Mathf.Min(points.Length - 1, controlPoints.Length);
            float scaledT = clampedT * segmentCount;
            int segmentIndex = Mathf.Min(Mathf.FloorToInt(scaledT), segmentCount - 1);
            float segmentT = scaledT - segmentIndex;

            return EvaluateQuadratic(
                points[segmentIndex],
                controlPoints[segmentIndex],
                points[segmentIndex + 1],
                segmentT);
        }

        /// <summary>
        /// Approximates the total length of a segmented quadratic path.
        /// </summary>
        public static float ApproximateSegmentedQuadraticPathLength(Vector2[] points, Vector2[] controlPoints, int samplesPerSegment = 12)
        {
            if (points == null || points.Length < 2 || controlPoints == null || controlPoints.Length == 0)
            {
                return 0f;
            }

            int segmentCount = Mathf.Min(points.Length - 1, controlPoints.Length);
            float length = 0f;
            for (int segmentIndex = 0; segmentIndex < segmentCount; segmentIndex++)
            {
                length += ApproximateQuadraticLength(
                    points[segmentIndex],
                    controlPoints[segmentIndex],
                    points[segmentIndex + 1],
                    samplesPerSegment);
            }

            return length;
        }
    }
}
