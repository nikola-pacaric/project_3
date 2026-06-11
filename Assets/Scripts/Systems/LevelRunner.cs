using UnityEngine;
using Warblade.Data;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Warblade.Systems
{
    /// <summary>
    /// Scene helper for previewing every wave referenced by a LevelData asset.
    /// </summary>
    public class LevelRunner : MonoBehaviour
    {
        [SerializeField] private LevelData _levelData;
        [SerializeField, Min(0)] private int _previewEnemyCount;
        [SerializeField] private bool _drawGizmos = true;
        [SerializeField] private bool _drawLabels = true;
        [SerializeField, Min(4)] private int _gizmoCurveSamples = 24;

        private void OnValidate()
        {
            RefreshPreviewEnemyCount();
        }

        private void OnDrawGizmos()
        {
            RefreshPreviewEnemyCount();

            if (!_drawGizmos || _levelData == null || _levelData.Waves == null)
            {
                return;
            }

            for (int waveIndex = 0; waveIndex < _levelData.Waves.Count; waveIndex++)
            {
                WaveData wave = _levelData.Waves[waveIndex];
                if (wave == null)
                {
                    continue;
                }

                DrawWaveGizmos(wave, waveIndex);
            }
        }

        private void RefreshPreviewEnemyCount()
        {
            _previewEnemyCount = CountEnemiesInLevelData(_levelData);
        }

        private static int CountEnemiesInLevelData(LevelData levelData)
        {
            if (levelData == null || levelData.Waves == null)
            {
                return 0;
            }

            int enemyCount = 0;
            for (int i = 0; i < levelData.Waves.Count; i++)
            {
                WaveData wave = levelData.Waves[i];
                if (wave == null)
                {
                    continue;
                }

                enemyCount += wave.SlotCount;
            }

            return enemyCount;
        }

        private void DrawWaveGizmos(WaveData wave, int waveIndex)
        {
            int slotCount = wave.SlotCount;
            if (slotCount <= 0)
            {
                return;
            }

            Vector2 anchor = wave.FormationAnchorPosition;
            Vector2 entryStartCenter = wave.EntryStartCenter;
            Vector2 entrySpacingStep = wave.EntrySpacingStep;
            float halfSpan = (slotCount - 1) * 0.5f;

            Color waveColor = Color.HSVToRGB((waveIndex * 0.173f) % 1f, 0.65f, 1f);
            Gizmos.color = waveColor;
            DrawCross(anchor, 0.22f);
            Gizmos.DrawWireSphere(entryStartCenter, 0.12f);

            DrawLabel(anchor + Vector2.up * 0.32f, $"Wave {waveIndex + 1}: {wave.name}", waveColor);
            DrawLabel(entryStartCenter + Vector2.up * 0.18f, $"W{waveIndex + 1} Entry", waveColor);

            if (wave.UsesWaypointEntryPath)
            {
                Vector2[] sharedPathPoints = wave.BuildSharedEntryPathWorldPoints(entryStartCenter);
                Vector2[] sharedControlPoints = wave.BuildEntryPathWorldControlPoints(sharedPathPoints);
                DrawSegmentedQuadraticPath(sharedPathPoints, sharedControlPoints, _gizmoCurveSamples);
                DrawSegmentControlPoints(sharedPathPoints, sharedControlPoints);

                for (int waypointIndex = 0; waypointIndex < wave.EntryPathWaypointCount; waypointIndex++)
                {
                    Vector2 waypoint = wave.GetEntryPathWaypointWorldPosition(waypointIndex);
                    Gizmos.DrawWireSphere(waypoint, 0.14f);
                    DrawCross(waypoint, 0.08f);
                    DrawLabel(waypoint + Vector2.up * 0.16f, $"W{waveIndex + 1}.{waypointIndex + 1}", waveColor);
                }
            }

            for (int slotIndex = 0; slotIndex < slotCount; slotIndex++)
            {
                WaveData.WaveSlot slot = wave.GetSlot(slotIndex);
                Vector2 start = entryStartCenter + entrySpacingStep * (slotIndex - halfSpan);
                Vector2 end = wave.GetSlotWorldPosition(slotIndex);

                if (wave.UsesWaypointEntryPath)
                {
                    Vector2 branchStart = wave.GetEntryPathEndWorldPosition(entryStartCenter);
                    Vector2 branchControl = wave.GetEntryPathBranchControlPoint(branchStart, end);
                    DrawQuadraticPath(branchStart, branchControl, end, _gizmoCurveSamples);
                    DrawBranchControlPoint(branchStart, branchControl, end);
                }
                else
                {
                    Vector2 control = ((start + end) * 0.5f) + slot.EntryControlOffset;
                    DrawQuadraticPath(start, control, end, _gizmoCurveSamples);
                    Gizmos.DrawSphere(start, 0.05f);
                }

                Gizmos.DrawWireSphere(end, 0.10f);
            }
        }

        private static void DrawCross(Vector2 center, float size)
        {
            Gizmos.DrawLine(center + Vector2.left * size, center + Vector2.right * size);
            Gizmos.DrawLine(center + Vector2.down * size, center + Vector2.up * size);
        }

        private static void DrawQuadraticPath(Vector2 start, Vector2 control, Vector2 end, int samples)
        {
            int clampedSamples = Mathf.Max(samples, 4);
            Vector2 previous = start;
            for (int i = 1; i <= clampedSamples; i++)
            {
                float t = i / (float)clampedSamples;
                Vector2 point = BezierPath.EvaluateQuadratic(start, control, end, t);
                Gizmos.DrawLine(previous, point);
                previous = point;
            }
        }

        private static void DrawSegmentedQuadraticPath(Vector2[] points, Vector2[] controlPoints, int samples)
        {
            if (points == null || points.Length < 2 || controlPoints == null || controlPoints.Length == 0)
            {
                return;
            }

            int clampedSamples = Mathf.Max(samples, 4);
            Vector2 previous = points[0];
            for (int i = 1; i <= clampedSamples; i++)
            {
                float t = i / (float)clampedSamples;
                Vector2 point = BezierPath.EvaluateSegmentedQuadraticPath(points, controlPoints, t);
                Gizmos.DrawLine(previous, point);
                previous = point;
            }
        }

        private static void DrawSegmentControlPoints(Vector2[] points, Vector2[] controlPoints)
        {
            if (points == null || controlPoints == null)
            {
                return;
            }

            Color previousColor = Gizmos.color;
            Gizmos.color = Color.yellow;
            int segmentCount = Mathf.Min(points.Length - 1, controlPoints.Length);
            for (int i = 0; i < segmentCount; i++)
            {
                Gizmos.DrawWireSphere(controlPoints[i], 0.07f);
                Gizmos.DrawLine(points[i], controlPoints[i]);
                Gizmos.DrawLine(controlPoints[i], points[i + 1]);
            }

            Gizmos.color = previousColor;
        }

        private static void DrawBranchControlPoint(Vector2 start, Vector2 control, Vector2 end)
        {
            Color previousColor = Gizmos.color;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(control, 0.07f);
            Gizmos.DrawLine(start, control);
            Gizmos.DrawLine(control, end);
            Gizmos.color = previousColor;
        }

        private void DrawLabel(Vector2 position, string text, Color color)
        {
#if UNITY_EDITOR
            if (!_drawLabels)
            {
                return;
            }

            GUIStyle style = new GUIStyle(EditorStyles.boldLabel)
            {
                normal = { textColor = color }
            };
            Handles.Label(position, text, style);
#endif
        }
    }
}
