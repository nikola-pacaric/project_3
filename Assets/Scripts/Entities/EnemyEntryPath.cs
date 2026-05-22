using UnityEngine;
using Warblade.Systems;

namespace Warblade.Entities
{
    internal sealed class EnemyEntryPath
    {
        private Vector2 _controlOffset;
        private Vector2[] _points;
        private Vector2[] _controlPoints;
        private Vector2[] _controlOffsets;

        public Vector2 Start { get; private set; }
        public Vector2 ControlPoint { get; private set; }
        public Vector2 End { get; private set; }
        public bool HasSegmentedPath => _points != null;
        public Vector2[] Points => _points;
        public Vector2[] ControlPoints => _controlPoints;

        public float Begin(
            Vector2 start,
            Vector2 end,
            Vector2 controlOffset,
            Vector2[] pathPoints,
            Vector2[] pathControlPoints,
            float speed)
        {
            Start = start;
            End = end;
            _controlOffset = controlOffset;

            bool hasSegmentedPath =
                pathPoints != null &&
                pathPoints.Length >= 2 &&
                pathControlPoints != null &&
                pathControlPoints.Length >= pathPoints.Length - 1;

            _points = hasSegmentedPath ? pathPoints : null;
            _controlPoints = hasSegmentedPath ? pathControlPoints : null;
            _controlOffsets = hasSegmentedPath
                ? BuildControlOffsets(pathPoints, pathControlPoints)
                : null;

            RetargetEnd(end);
            return CalculateDuration(speed);
        }

        public void RetargetEnd(Vector2 end)
        {
            End = end;

            if (_points != null)
            {
                _points[0] = Start;
                _points[_points.Length - 1] = End;
                RefreshSegmentedPathControlPoints();
                ControlPoint = _controlPoints != null && _controlPoints.Length > 0
                    ? _controlPoints[0]
                    : (Start + End) * 0.5f;
                return;
            }

            Vector2 midpoint = (Start + End) * 0.5f;
            ControlPoint = midpoint + _controlOffset;
        }

        public Vector2 Evaluate(float normalizedTime)
        {
            float t = Mathf.Clamp01(normalizedTime);
            return _points != null
                ? BezierPath.EvaluateSegmentedQuadraticPath(_points, _controlPoints, t)
                : BezierPath.EvaluateQuadratic(Start, ControlPoint, End, t);
        }

        public void ClearSegmentedPath()
        {
            _points = null;
            _controlPoints = null;
            _controlOffsets = null;
        }

        public float CalculateDuration(float speed)
        {
            float pathLength = _points != null
                ? BezierPath.ApproximateSegmentedQuadraticPathLength(_points, _controlPoints)
                : BezierPath.ApproximateQuadraticLength(Start, ControlPoint, End);

            return pathLength / Mathf.Max(speed, 0.01f);
        }

        private static Vector2[] BuildControlOffsets(Vector2[] points, Vector2[] controlPoints)
        {
            if (points == null || controlPoints == null)
            {
                return null;
            }

            int segmentCount = Mathf.Min(points.Length - 1, controlPoints.Length);
            Vector2[] offsets = new Vector2[segmentCount];
            for (int i = 0; i < segmentCount; i++)
            {
                Vector2 midpoint = (points[i] + points[i + 1]) * 0.5f;
                offsets[i] = controlPoints[i] - midpoint;
            }

            return offsets;
        }

        private void RefreshSegmentedPathControlPoints()
        {
            if (_points == null || _controlPoints == null || _controlOffsets == null)
            {
                return;
            }

            int segmentCount = Mathf.Min(
                _points.Length - 1,
                Mathf.Min(_controlPoints.Length, _controlOffsets.Length));

            for (int i = 0; i < segmentCount; i++)
            {
                Vector2 midpoint = (_points[i] + _points[i + 1]) * 0.5f;
                _controlPoints[i] = midpoint + _controlOffsets[i];
            }
        }
    }
}
